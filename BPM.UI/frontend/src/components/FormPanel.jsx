import React, { useState, useEffect, useCallback } from 'react';
import { fetchSchema, executeCommand } from '../api';

const schemaCache = {};

export default function FormPanel({ commandName, endpoint, httpMethod, processId, onSuccess, onClose }) {
  const [schema, setSchema] = useState(null);
  const [values, setValues] = useState({});
  const [errors, setErrors] = useState({});
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState(null);
  const [isInfoMode, setIsInfoMode] = useState(false);
  const [infoData, setInfoData] = useState(null);

  useEffect(() => {
    if (!commandName) return;

    if (httpMethod === 'GET') {
      setIsInfoMode(true);
      loadInfoData();
      return;
    }

    setIsInfoMode(false);
    loadSchema();
  }, [commandName]);

  async function loadSchema() {
    try {
      if (schemaCache[commandName]) {
        setSchema(schemaCache[commandName]);
        initValues(schemaCache[commandName]);
        return;
      }
      const data = await fetchSchema(commandName);
      schemaCache[commandName] = data;
      setSchema(data);
      initValues(data);
    } catch (e) {
      setSubmitError(`Failed to load schema: ${e.message}`);
    }
  }

  async function loadInfoData() {
    try {
      const url = endpoint.replace('{processId}', processId);
      const res = await fetch(url);
      const data = await res.json();
      setInfoData(data);
    } catch (e) {
      setSubmitError(`Failed to load data: ${e.message}`);
    }
  }

  function initValues(schema) {
    const initial = {};
    for (const field of schema.fields || []) {
      if (field.name.toLowerCase() === 'processid') continue;
      if (field.isHidden) continue;
      if (field.type === 'Boolean') initial[field.name] = false;
      else if (field.type === 'Number' || field.type === 'Decimal') initial[field.name] = '';
      else initial[field.name] = '';
    }
    setValues(initial);
    setErrors({});
    setSubmitError(null);
  }

  function validate() {
    const errs = {};
    if (!schema?.fields) return errs;

    for (const field of schema.fields) {
      if (field.name.toLowerCase() === 'processid') continue;
      if (field.isHidden) continue;

      const val = values[field.name];

      if (field.isRequired && (val === '' || val === null || val === undefined)) {
        errs[field.name] = `${field.label} is required`;
        continue;
      }

      if (field.regex && val && typeof val === 'string') {
        const re = new RegExp(field.regex);
        if (!re.test(val)) {
          errs[field.name] = `Invalid format`;
        }
      }

      if (field.min !== null && field.min !== undefined && val !== '') {
        const num = parseFloat(val);
        if (!isNaN(num) && num < field.min) {
          errs[field.name] = `Minimum value is ${field.min}`;
        }
      }

      if (field.max !== null && field.max !== undefined && val !== '') {
        const num = parseFloat(val);
        if (!isNaN(num) && num > field.max) {
          errs[field.name] = `Maximum value is ${field.max}`;
        }
      }
    }
    return errs;
  }

  async function handleSubmit(e) {
    e.preventDefault();
    const validationErrors = validate();
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSubmitting(true);
    setSubmitError(null);

    try {
      const payload = {};

      for (const field of schema.fields || []) {
        if (field.name.toLowerCase() === 'processid') {
          if (processId) payload[field.name] = processId;
          continue;
        }
        if (field.isHidden) {
          payload[field.name] = getDefaultForType(field.type);
          continue;
        }

        let val = values[field.name];
        if (field.type === 'Number') val = val !== '' ? parseInt(val, 10) : 0;
        else if (field.type === 'Decimal') val = val !== '' ? parseFloat(val) : 0;
        else if (field.type === 'Boolean') val = !!val;
        else if (field.type === 'Guid' && !val) val = '00000000-0000-0000-0000-000000000000';

        payload[field.name] = val;
      }

      const result = await executeCommand(commandName, payload);

      if (result.success) {
        onSuccess?.(result);
      } else {
        setSubmitError(result.error || 'Command execution failed');
      }
    } catch (e) {
      setSubmitError(e.message);
    } finally {
      setSubmitting(false);
    }
  }

  function getDefaultForType(type) {
    switch (type) {
      case 'Number': return 0;
      case 'Decimal': return 0.0;
      case 'Boolean': return false;
      case 'Guid': return '00000000-0000-0000-0000-000000000000';
      case 'DateTime': return new Date().toISOString();
      default: return '';
    }
  }

  function renderField(field) {
    if (field.name.toLowerCase() === 'processid') return null;
    if (field.isHidden) return null;

    const hasError = errors[field.name];

    switch (field.type) {
      case 'Boolean':
        return (
          <div className="form-group" key={field.name}>
            <label>{field.label}</label>
            <div className="toggle-switch">
              <input
                type="checkbox"
                checked={!!values[field.name]}
                onChange={(e) => setValues({ ...values, [field.name]: e.target.checked })}
              />
              <span style={{ fontSize: 13, color: '#6b7280' }}>
                {values[field.name] ? 'Yes' : 'No'}
              </span>
            </div>
          </div>
        );

      case 'Select':
        return (
          <div className="form-group" key={field.name}>
            <label>{field.label}{field.isRequired && ' *'}</label>
            <select
              value={values[field.name] || ''}
              onChange={(e) => setValues({ ...values, [field.name]: e.target.value })}
              className={hasError ? 'invalid' : ''}
            >
              <option value="">Select...</option>
              {(field.options || []).map((opt) => (
                <option key={opt} value={opt}>{opt}</option>
              ))}
            </select>
            {hasError && <div className="error">{errors[field.name]}</div>}
          </div>
        );

      case 'DateTime':
        return (
          <div className="form-group" key={field.name}>
            <label>{field.label}{field.isRequired && ' *'}</label>
            <input
              type="datetime-local"
              value={values[field.name] || ''}
              onChange={(e) => setValues({ ...values, [field.name]: e.target.value })}
              className={hasError ? 'invalid' : ''}
              placeholder={field.placeholder}
            />
            {hasError && <div className="error">{errors[field.name]}</div>}
          </div>
        );

      case 'File':
        return (
          <div className="form-group" key={field.name}>
            <label>{field.label}{field.isRequired && ' *'}</label>
            <input
              type="file"
              onChange={(e) => setValues({ ...values, [field.name]: e.target.files[0] })}
              className={hasError ? 'invalid' : ''}
            />
            {hasError && <div className="error">{errors[field.name]}</div>}
          </div>
        );

      case 'Number':
      case 'Decimal':
        return (
          <div className="form-group" key={field.name}>
            <label>{field.label}{field.isRequired && ' *'}</label>
            <input
              type="number"
              step={field.type === 'Decimal' ? '0.01' : '1'}
              value={values[field.name] ?? ''}
              min={field.min}
              max={field.max}
              onChange={(e) => setValues({ ...values, [field.name]: e.target.value })}
              className={hasError ? 'invalid' : ''}
              placeholder={field.placeholder}
            />
            {hasError && <div className="error">{errors[field.name]}</div>}
          </div>
        );

      default:
        return (
          <div className="form-group" key={field.name}>
            <label>{field.label}{field.isRequired && ' *'}</label>
            <input
              type="text"
              value={values[field.name] || ''}
              onChange={(e) => setValues({ ...values, [field.name]: e.target.value })}
              className={hasError ? 'invalid' : ''}
              placeholder={field.placeholder}
            />
            {hasError && <div className="error">{errors[field.name]}</div>}
          </div>
        );
    }
  }

  if (isInfoMode) {
    return (
      <div className={`slide-panel open`}>
        <button className="close-btn" onClick={onClose}>&times;</button>
        <h2>{humanize(commandName)}</h2>
        {submitError && (
          <div style={{ color: '#ef4444', marginBottom: 12, fontSize: 13 }}>{submitError}</div>
        )}
        {infoData ? (
          <div className="info-panel">
            {Object.entries(infoData).map(([key, value]) => (
              <div className="info-row" key={key}>
                <span className="info-key">{humanize(key)}</span>
                <span className="info-value">{String(value)}</span>
              </div>
            ))}
          </div>
        ) : (
          <div style={{ color: '#6b7280' }}>Loading...</div>
        )}
      </div>
    );
  }

  return (
    <div className={`slide-panel open`}>
      <button className="close-btn" onClick={onClose}>&times;</button>
      <h2>{humanize(commandName)}</h2>
      {!schema ? (
        <div style={{ color: '#6b7280' }}>Loading schema...</div>
      ) : (
        <form onSubmit={handleSubmit}>
          {(schema.fields || []).map(renderField)}
          {submitError && (
            <div style={{ color: '#ef4444', marginBottom: 8, fontSize: 13 }}>{submitError}</div>
          )}
          <button type="submit" className="submit-btn" disabled={submitting}>
            {submitting ? 'Executing...' : 'Execute'}
          </button>
        </form>
      )}
    </div>
  );
}

function humanize(name) {
  if (!name) return '';
  return name.replace(/([A-Z])/g, ' $1').trim();
}
