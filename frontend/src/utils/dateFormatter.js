

export const formatDate = (dateInput) => {
  if (!dateInput) return 'N/A';
  const date = new Date(dateInput);
  return new Intl.DateTimeFormat('en-US', {
    placeholder: 'MM/DD/YYYY',
    year: 'numeric',
    month: 'short',
    day: '2-digit'
  }).format(date);
};

export const formatTime = (dateInput) => {
  if (!dateInput) return 'N/A';
  const date = new Date(dateInput);
  return new Intl.DateTimeFormat('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  }).format(date);
};

export const formatFullMonth = (dateInput) => {
  if (!dateInput) return ''
  const date = dateInput.includes('T') ? new Date(dateInput) : new Date(`${dateInput}T00:00:00`)
  return new Intl.DateTimeFormat('en-US', { month: 'long' }).format(date)
}


export const formatDayOnly = (dateInput) => {
  if (!dateInput) return ''
  const date = dateInput.includes('T') ? new Date(dateInput) : new Date(`${dateInput}T00:00:00`)
  return new Intl.DateTimeFormat('en-US', { day: '2-digit' }).format(date)
}

export const formatFullDateTime = (dateInput) => {
  if (!dateInput) return 'N/A';
  const date = new Date(dateInput);
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  }).format(date);
};




export const toLocalDateString = (date) => {
  if (!date) return '';
  const d = date instanceof Date ? date : new Date(date);
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

export default {
  date: formatDate,
  time: formatTime,
  full: formatFullDateTime,
  fullMonth: formatFullMonth,
  dayOnly: formatDayOnly,
  toLocalDateString,
}
