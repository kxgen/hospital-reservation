<script setup>
import { computed } from 'vue'
import { formatTime } from '@/utils/dateFormatter'

const props = defineProps({
  appt: {
    type: Object,
    required: true
  },
  showAction: {
    type: Boolean,
    default: true
  },
  actionLabel: {
    type: String,
    default: 'Manage Appointment'
  },
  showConfirm: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['confirm'])

const formatDate = (dateStr, format) => {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  if (format === 'DD') return d.getDate()
  if (format === 'MMM') return d.toLocaleString('default', { month: 'short' }).toUpperCase()
  if (format === 'time') return formatTime(dateStr)
  return d.toLocaleString()
}

const statusClass = computed(() => `status-${props.appt.status.toLowerCase()}`)
</script>

<template>
  <div class="appointment-card">
    <div class="date-section">
      <span class="month">{{ formatDate(appt.startTime || appt.bookedAt, 'MMM') }}</span>
      <span class="day">{{ formatDate(appt.startTime || appt.bookedAt, 'DD') }}</span>
    </div>

    <div class="info-section">
      <div class="top-row">
        <h3>Dr. {{ appt.doctorName || 'Medical Professional' }}</h3>
        <span class="status-pill" :class="statusClass">
          <span class="dot"></span>
          {{ appt.status }}
        </span>
      </div>
      
      <div class="bottom-row">
        <span class="specialty">{{ appt.specialty || 'General Consultation' }}</span>
        <span class="separator">/</span>
        <span class="time-slot">
          <span class="clock-icon">🕒</span> 
          {{ formatDate(appt.startTime || appt.bookedAt, 'time') }}
        </span>
      </div>
    </div>

    <div v-if="showAction" class="action-section">
      <div class="actions-group">
        <button v-if="showConfirm && appt.status.toLowerCase() === 'scheduled'" @click="emit('confirm', appt.id)" class="btn-confirm">
          Confirm Now
        </button>
        <router-link :to="'/patient/edit-appointment/' + appt.id" class="btn-manage">
          {{ actionLabel }}
        </router-link>
      </div>
    </div>
  </div>
</template>

<style scoped>
.appointment-card {
  background: var(--color-white);
  border-radius: 6px;
  padding: 24px;
  display: grid;
  grid-template-columns: auto 1fr auto;
  align-items: center;
  gap: 32px;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: var(--shadow-sm);
}

.appointment-card:hover {
  border-color: var(--color-primary);
  box-shadow: var(--shadow-md);
}

/* Date Section Styling */
.date-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding-right: 32px;
  border-right: 2px solid var(--color-gray-50);
  min-width: 80px;
}

.date-section .month {
  font-size: 0.75rem;
  font-weight: 800;
  color: var(--color-primary);
  letter-spacing: 1.5px;
}

.date-section .day {
  font-size: 2.2rem;
  font-weight: 800;
  color: var(--text-main);
  line-height: 1;
  margin-top: 4px;
}

/* Info Section Styling */
.info-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.top-row {
  display: flex;
  align-items: center;
  gap: 16px;
}

.top-row h3 {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-main);
  letter-spacing: -0.01em;
}

/* Status Pill Enhancements */
.status-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 0.7rem;
  font-weight: 700;
  text-transform: uppercase;
  padding: 6px 12px;
  border-radius: 8px;
  letter-spacing: 0.5px;
}

.dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentColor;
}

.status-confirmed { 
  background: var(--color-primary-light); 
  color: var(--color-primary); 
}

.status-scheduled {
  background: #fef3c7;
  color: #92400e;
}

.status-cancelled { 
  background: #fff1f1; 
  color: var(--text-error); 
}

.status-completed { 
  background: var(--color-gray-100); 
  color: var(--text-muted); 
}

.status-pending {
  background: #f3e8ff;
  color: #7e22ce;
}

/* Metadata Styling */
.bottom-row {
  display: flex;
  align-items: center;
  font-size: 0.95rem;
  color: var(--text-muted);
  font-weight: 500;
}

.separator { 
  margin: 0 12px; 
  color: var(--color-gray-300); 
  font-weight: 300;
}

.time-slot {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--color-gray-700);
}

.clock-icon {
  font-size: 1rem;
  filter: grayscale(1);
  opacity: 0.6;
}

/* Manage Button Styling */
.btn-manage {
  display: inline-block;
  padding: 10px 24px;
  background: var(--color-white);
  color: var(--color-primary);
  border: 1.5px solid var(--color-primary);
  border-radius: 10px;
  font-size: 0.9rem;
  font-weight: 700;
  text-decoration: none;
  transition: all 0.2s;
}

.btn-manage:hover {
  background: var(--color-primary);
  color: var(--color-white);
  box-shadow: var(--shadow-primary);
}

.actions-group {
  display: flex;
  gap: 12px;
  align-items: center;
}

.btn-confirm {
  padding: 10px 24px;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 10px;
  font-size: 0.9rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-confirm:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-primary);
}

/* Responsive adjustments */
@media (max-width: 640px) {
  .appointment-card {
    grid-template-columns: 1fr;
    gap: 20px;
    text-align: center;
  }
  .date-section {
    border-right: none;
    border-bottom: 2px solid var(--color-gray-50);
    padding-right: 0;
    padding-bottom: 16px;
  }
  .top-row {
    flex-direction: column;
    gap: 8px;
  }
}
</style>