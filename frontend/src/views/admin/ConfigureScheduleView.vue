<template>
  <div class="admin-container">
    <div class="page-header">
      <div class="header-content">
        <router-link to="/admin/accounts" class="btn-back">
          <span class="icon">←</span> Back to Accounts
        </router-link>
        <h1>Manage <span>Availability</span></h1>
        <p v-if="doctorName">Define recurring weekly hours for <strong>{{ doctorName }}</strong></p>
      </div>
      <div class="header-actions">
        <button class="btn-primary" @click="saveSchedule" :disabled="saving">
          <span v-if="saving" class="spinner"></span>
          {{ saving ? 'Saving Changes...' : 'Save Weekly Schedule' }}
        </button>
      </div>
    </div>

    <div class="content-wrapper">
      <div class="info-banner">
        <div class="banner-icon">💡</div>
        <div class="banner-text">
          <strong>How it works:</strong> The system uses these recurring weekly hours to dynamically generate available time slots for patients. Changes made here apply immediately to all upcoming dates.
        </div>
      </div>

      <div class="content-card">


        <div class="schedule-grid">
          <div
            v-for="day in days"
            :key="day"
            :class="['day-card', { active: isAvailable(day) }]"
          >
            <div class="day-header" @click="toggleDay(day)">
              <div class="day-check">
                <div class="checkbox-ui" :class="{ checked: isAvailable(day) }">
                  <span v-if="isAvailable(day)">✓</span>
                </div>
                <span class="day-name">{{ day }}</span>
              </div>
              <div v-if="isAvailable(day)" class="day-actions">
                <button class="action-btn" @click.stop="copyToAll(day)" title="Copy this schedule to all other active days">
                   <span class="icon">⧉</span> Copy to all
                </button>
              </div>
            </div>

            <div class="day-body">
              <div v-if="isAvailable(day)" class="intervals-list">
                <div
                  v-for="(slot, index) in scheduleData[day]"
                  :key="index"
                  class="interval-item"
                >
                  <div class="time-picker-group">
                    <input type="time" v-model="slot.start" class="time-input" />
                    <span class="separator">to</span>
                    <input type="time" v-model="slot.end" class="time-input" />
                  </div>

                  <button
                    class="btn-remove"
                    @click="removeSlot(day, index)"
                    title="Remove interval"
                  >
                    ×
                  </button>
                </div>

                <button class="btn-add-interval" @click="addSlot(day)">
                  <span class="plus">+</span> Add Interval
                </button>
              </div>

              <div v-else class="closed-state">
                <span class="text">Not Working</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import axios from '@/api/axios'
import { useAlertStore } from '@/stores/alertStore'

/* -------------------- CONSTANTS -------------------- */
const DEFAULT_START_TIME = '08:00'
const DEFAULT_DURATION = 30
const days = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday']

/* -------------------- STATE -------------------- */
const route = useRoute()
const doctorId = route.params.id
const doctorName = ref('')
const saving = ref(false)
const scheduleData = ref({
  monday: null, tuesday: null, wednesday: null, thursday: null, friday: null, saturday: null, sunday: null
})
const alerts = useAlertStore()



/* -------------------- HELPERS -------------------- */
const addMinutes = (time, minutes) => {
  if (!time) return '09:00'
  const [h, m] = time.split(':').map(Number)
  const d = new Date(0, 0, 0, h, m + minutes)
  return d.toTimeString().slice(0, 5)
}

const isAvailable = (day) => scheduleData.value[day] !== null

/* -------------------- LOGIC -------------------- */
const toggleDay = (day) => {
  if (isAvailable(day)) {
    scheduleData.value[day] = null
  } else {
    // Initialize as empty array to mark as active/open
    scheduleData.value[day] = []
  }
}

const addSlot = (day) => {
  const slots = scheduleData.value[day]
  let startTime = DEFAULT_START_TIME
  if (slots.length > 0) {
    startTime = slots[slots.length - 1].end
  }
  slots.push({
    start: startTime,
    end: addMinutes(startTime, DEFAULT_DURATION)
  })
}

const removeSlot = (day, index) => {
  scheduleData.value[day].splice(index, 1)
}

const copyToAll = (sourceDay) => {
  const sourceSlots = JSON.parse(JSON.stringify(scheduleData.value[sourceDay]))
  days.forEach(day => {
    // Only copy to days that are currently "On" (Active)
    if (day !== sourceDay && isAvailable(day)) {
      scheduleData.value[day] = JSON.parse(JSON.stringify(sourceSlots))
    }
  })
  alerts.showAlert(`Copied schedule to all active days`, 'success')
}

const validateSchedule = () => {
  for (const day of days) {
    const slots = scheduleData.value[day]
    if (!slots || slots.length === 0) continue

    // Sort to check overlaps
    const sorted = [...slots].sort((a, b) => a.start.localeCompare(b.start))
    
    for (let i = 0; i < sorted.length; i++) {
      const slot = sorted[i]
      
      // 1. Chronological order & Gap validation
      if (slot.end <= slot.start) {
        alerts.showAlert(`Invalid range on ${day}: ${slot.start} to ${slot.end}. End time must be after start time.`, 'error')
        return false
      }
      
      // 2. Overlap validation
      if (i > 0) {
        const prev = sorted[i - 1]
        if (prev.end > slot.start) {
          alerts.showAlert(`Overlap detected on ${day}: ${slot.start} starts before previous slot ends at ${prev.end}`, 'error')
          return false
        }
      }
    }
  }
  return true
}

/* -------------------- API -------------------- */
const saveSchedule = async () => {
  if (!validateSchedule()) return
  
  saving.value = true
  try {
    const token = localStorage.getItem('token')
    
    // Format payload for backend: TitleCase keys and Schedule wrapper
    const payload = {
      Schedule: {}
    }
    
    Object.keys(scheduleData.value).forEach(day => {
      const daySlots = scheduleData.value[day]
      if (daySlots !== null) {
        payload.Schedule[day] = daySlots.map(s => ({
          Start: s.start,
          End: s.end
        }))
      }
    })

    await axios.post(
      `/api/admin/doctors/${doctorId}/availability`,
      payload,
      { headers: { Authorization: `Bearer ${token}` } }
    )
    alerts.showAlert('Weekly schedule updated successfully', 'success')
  } catch (error) {
    console.error('Save failed:', error)
    alerts.showAlert('Failed to save schedule', 'error')
  } finally {
    saving.value = false
  }
}

const loadAvailability = async () => {
  try {
    const token = localStorage.getItem('token')
    
    // Load doctor name
    const dr = await axios.get(
      `/api/admin/staff/${doctorId}`,
      { headers: { Authorization: `Bearer ${token}` } }
    )
    doctorName.value = `Dr. ${dr.data.firstName} ${dr.data.lastName}`

    // Load schedule
    const res = await axios.get(
      `/api/admin/doctors/${doctorId}/availability`,
      { headers: { Authorization: `Bearer ${token}` } }
    )

    if (res.data?.schedule) {
      // First, ensure everything is off (null) before applying loaded data
      days.forEach(d => scheduleData.value[d] = null)

      Object.keys(res.data.schedule).forEach(day => {
        const d = day.toLowerCase()
        if (scheduleData.value[d] !== undefined) {
          const rawDayData = res.data.schedule[day] || []
          
          // CRITICAL: Only mark as active if there are actual intervals
          // This keeps empty days "Not Active" (Checkbox Unchecked)
          if (rawDayData.length > 0) {
            scheduleData.value[d] = rawDayData.map(interval => ({
              start: (interval.start || interval.Start || '').slice(0, 5),
              end: (interval.end || interval.End || '').slice(0, 5)
            }))
          } else {
            scheduleData.value[d] = null
          }
        }
      })
    }
  } catch (err) {
    console.error('Load failed:', err)
  }
}

/* -------------------- UI -------------------- */
// Removed local showToast helper as it's replaced by alertStore

onMounted(loadAvailability)
</script>

<style scoped>
.admin-container { animation: fadeIn 0.4s ease-out; }

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.btn-back {
  color: var(--color-primary);
  text-decoration: none;
  font-weight: 700;
  font-size: 0.9rem;
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
}

/* Info Banner */
.info-banner {
  background: var(--bg-status-info);
  border: 1px solid var(--border-default);
  color: var(--status-info-text);
  padding: 20px;
  border-radius: 12px;
  display: flex;
  gap: 15px;
  margin-bottom: 40px;
}

.banner-icon { font-size: 1.5rem; }
.banner-text { font-size: 0.95rem; line-height: 1.5; }

/* Schedule Grid */
.schedule-grid { display: flex; flex-direction: column; }

.day-card {
  border-bottom: 1px solid var(--border-default);
  display: grid;
  grid-template-columns: 240px 1fr;
  min-height: 100px;
}

.day-card:last-child { border-bottom: none; }

.day-header {
  padding: 30px;
  background: var(--bg-body);
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 15px;
  cursor: pointer;
  transition: background 0.2s;
}

.day-card.active .day-header { background: white; }

.day-check { display: flex; align-items: center; gap: 15px; }

.checkbox-ui {
  width: 24px;
  height: 24px;
  border: 2px solid var(--border-input);
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 900;
  color: white;
  transition: all 0.2s;
}

.checkbox-ui.checked { background: var(--color-primary); border-color: var(--color-primary); }

.day-name { font-size: 1.1rem; font-weight: 800; text-transform: capitalize; color: var(--text-primary); }

.action-btn {
  background: none;
  border: none;
  color: var(--color-primary);
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
  padding: 0;
  display: flex;
  align-items: center;
  gap: 5px;
}

.action-btn:hover { text-decoration: underline; }

.day-body { padding: 30px; display: flex; align-items: center; }

/* Intervals */
.intervals-list { display: flex; flex-direction: column; gap: 12px; width: 100%; }

.interval-item {
  display: flex;
  align-items: center;
  gap: 20px;
  background: var(--bg-input);
  padding: 10px 15px;
  border-radius: 12px;
  width: fit-content;
}

.time-picker-group { display: flex; align-items: center; gap: 15px; }

.time-input {
  background: white;
  border: 1px solid var(--border-default);
  padding: 8px 12px;
  border-radius: 8px;
  font-family: inherit;
  font-weight: 700;
  font-size: 0.95rem;
  outline: none;
}

.time-input:focus { border-color: var(--color-primary); box-shadow: 0 0 0 3px var(--color-primary-glow); }

.separator { font-weight: 700; color: var(--text-tertiary); text-transform: uppercase; font-size: 0.7rem; }

.btn-remove {
  background: white;
  border: 1px solid var(--border-default);
  width: 28px; height: 28px;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  cursor: pointer; color: var(--color-error); font-size: 1.2rem;
  transition: all 0.2s;
}

.btn-remove:hover { background: var(--color-error); color: white; border-color: var(--color-error); }

.btn-add-interval {
  background: none;
  border: 2px dashed var(--border-default);
  padding: 12px 20px;
  border-radius: 10px;
  color: var(--color-primary);
  font-weight: 700;
  cursor: pointer;
  width: fit-content;
  transition: all 0.2s;
}

.btn-add-interval:hover { background: var(--color-primary-light); border-color: var(--color-primary); }

.closed-state { display: flex; align-items: center; gap: 10px; color: var(--text-tertiary); opacity: 0.6; font-weight: 600; }

.spinner {
  width: 16px; height: 16px; 
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: white; border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

@media (max-width: 900px) {
  .day-card { grid-template-columns: 1fr; }
  .day-header { padding: 20px; border-bottom: 1px solid var(--border-default); flex-direction: row; justify-content: space-between; }

}

.loader { text-align: center; padding: 100px; color: var(--text-tertiary); font-style: italic; }
</style>
