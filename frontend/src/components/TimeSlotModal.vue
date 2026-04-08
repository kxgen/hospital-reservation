<template>
  <div v-if="visible" class="modal-overlay" @click.self="emit('close')">
    <div class="modal-content">
      <div class="header-sticky-wrapper">
        <div class="brand-header">
          <div class="logo">
            <div class="logo-icon">
              <Logo />
            </div>
            <div class="logo-text">
              <span class="main">Trinity</span>
              <span class="suffix">Specialized Center</span>
            </div>
          </div>
          <button class="close-btn" @click="emit('close')">&times;</button>
        </div>
        <div class="doctor-info-header">
          <div class="doctor-context">
            <h2 class="doctor-name">{{ doctor.name }}</h2>
            <p class="doctor-specialty">{{ doctor.specialty }}</p>
          </div>
        </div>
      </div>

      <div class="appointments-section">
        <div v-if="displayedDays.length > 0">
          <div v-for="day in displayedDays" :key="day.date" class="day-slot">
            <div class="day-label">{{ formatDate(day.date) }}</div>
            <div class="slots-grid">
              <button
                v-for="slot in day.slots"
                :key="slot.Id"
                class="slot-btn"
                @click="selectSlot(slot)"
              >
                {{ formatTime(slot.StartTime) }}
              </button>
            </div>
          </div>
          
          <!-- See More Button -->
          <div v-if="!showAll && hasMoreDays" class="see-more-container">
            <button class="see-more-btn" @click="showAll = true">
              See More Availability...
            </button>
          </div>
        </div>
        <div v-else class="no-slots">No available appointments from this date onwards.</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { formatTime } from '@/utils/dateFormatter'
import { useAuthStore } from '@/stores/auth'
import Logo from '@/components/Logo.vue'

const props = defineProps({
  doctor: Object,
  visible: Boolean,
  appointments: Array,
  date: Date,
  emitOnly: { type: Boolean, default: false }
})

const emit = defineEmits(['close', 'time-selected'])
const router = useRouter()
const auth = useAuthStore()
const showAll = ref(false)

const daysWithSlots = computed(() => {
  if (!props.appointments || !Array.isArray(props.appointments)) return []
  const grouped = {}
  
  const startDate = new Date(props.date || new Date())
  startDate.setHours(0, 0, 0, 0)

  props.appointments.forEach(slot => {
    const sTime = slot.StartTime || slot.startTime || slot.start
    const available = slot.IsAvailable !== undefined ? slot.IsAvailable : (slot.isAvailable !== undefined ? slot.isAvailable : true)
    const sId = slot.Id || slot.id || 0
    if (!sTime) return

    const slotDate = new Date(sTime)
    if (available && slotDate >= startDate) {
      const dateKey = slotDate.toDateString()
      if (!grouped[dateKey]) {
        grouped[dateKey] = { date: slotDate, slots: [] }
      }
      grouped[dateKey].slots.push({ Id: sId, StartTime: sTime, EndTime: slot.EndTime || slot.endTime, IsAvailable: available })
    }
  })

  return Object.values(grouped)
    .sort((a, b) => a.date - b.date)
    .map(group => ({
      date: group.date,
      slots: group.slots.sort((a, b) => new Date(a.StartTime) - new Date(b.StartTime))
    }))
})

const displayedDays = computed(() => {
  return showAll.value ? daysWithSlots.value : daysWithSlots.value.slice(0, 1)
})

const hasMoreDays = computed(() => daysWithSlots.value.length > 1)

const formatDate = date => {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const tomorrow = new Date(today)
  tomorrow.setDate(today.getDate() + 1)

  if (date.toDateString() === today.toDateString()) return 'Today'
  if (date.toDateString() === tomorrow.toDateString()) return 'Tomorrow'

  return date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })
}

const selectSlot = slot => {
  const dateStr = formatDate(new Date(slot.StartTime))
  const timeStr = formatTime(slot.StartTime)

  if (props.emitOnly) {
    emit('time-selected', { 
        timeslotId: slot.Id, 
        startTime: slot.StartTime, 
        endTime: slot.EndTime, 
        timeLabel: `${dateStr} at ${timeStr}` 
    })
  } else {
    const isReceptionist = auth.role === 'receptionist';
    const targetPath = isReceptionist ? '/reception/book-form' : '/patient/book-appointment';
    
    router.push({
      path: targetPath,
      query: {
        doctorId: props.doctor.id,
        doctorName: props.doctor.name,
        specialty: props.doctor.specialty,
        timeslotId: slot.Id,
        startTime: slot.StartTime,
        endTime: slot.EndTime,
        timeLabel: `${dateStr} at ${timeStr}`
      }
    })
  }

  emit('close')
}
</script>

<style scoped>
.modal-overlay { position: fixed; top: 0; left: 0; width: 100vw; height: 100vh; background: rgba(0,0,0,0.6); display: flex; justify-content: center; align-items: center; z-index: 3000; backdrop-filter: blur(4px); }
.modal-content { background: var(--bg-card); border-radius: 0; width: 95%; max-width: 500px; max-height: 85vh; overflow-y: auto; padding: 0; box-shadow: var(--shadow-lg); border: 1px solid var(--border-default); }

.header-sticky-wrapper {
  position: sticky; 
  top: 0; 
  z-index: 10;
  background: var(--bg-card);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); /* stronger shadow for separation */
}

.brand-header {
  padding: 12px 24px;
  /* background: var(--bg-surface) is inherited/default */
  /* Remove border to make it look like one piece if desired, or keep for subtle separation within header */
  /* border-bottom: 1px solid var(--border-default); */ 
  display: flex;
  align-items: center;
  justify-content: space-between; /* Ensure logo and close button are on opposite ends */
}

.doctor-info-header { 
  display: flex; 
  justify-content: space-between; 
  align-items: flex-start; 
  padding: 10px 24px 20px 24px; /* Adjust padding to sit nicely under logo */
  background: var(--bg-card); /* Changed from primary to surface */
  border-bottom: 1px solid var(--border-default); 
}

/* Logo Styles */
.logo { display: flex; align-items: center; gap: 2px; text-decoration: none; user-select: none; }
.logo-icon {
  width: 28px; height: 28px; 
  background: var(--logo-bg);
  border-radius: 0;
  display: flex; align-items: center; justify-content: center; padding: 6px;
}
.logo-text { display: flex; flex-direction: column; justify-content: center; line-height: 1.1; }
.logo-text .main { font-size: 1.1rem; font-weight: 800; color: var(--text-primary); letter-spacing: -0.02em; }
.logo-text .suffix { font-size: 0.65rem; font-weight: 700; color: var(--color-primary); text-transform: uppercase; letter-spacing: 1px; }

.doctor-context {
  padding: 10px;
  display: flex; width: 100%;
  justify-content: space-between;
  align-items: center;
}

.doctor-context .doctor-name { 
  color: var(--text-primary); /* Changed from primary-light (white) to dark */
  margin: 0; 
  font-size: 1.1rem; 
  font-weight: 800;
}

.doctor-context .doctor-specialty { 
  color: var(--text-secondary); /* Changed from primary-light to muted */
  margin: 4px 0 0 0; 
  font-size: 0.9rem; 
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  padding-left: 5px;
}

.close-btn { 
  background: var(--color-gray-100); 
  border: none; 
  width: 32px; 
  height: 32px; 
  border-radius: 0; 
  font-size: 1.2rem; 
  cursor: pointer; 
  color: var(--text-secondary); 
  display: flex; 
  align-items: center; 
  justify-content: center;
  transition: all 0.2s;
}
.close-btn:hover { background: var(--border-default); color: var(--color-primary); }

.appointments-section { padding: 30px; }

.day-slot { margin-bottom: 30px; }
.day-slot:last-child { margin-bottom: 0; }

.day-label { 
  font-weight: 800; 
  margin-bottom: 12px; 
  font-size: 0.9rem; 
  color: var(--text-secondary); 
  text-transform: uppercase;
  letter-spacing: 1px;
}

.slots-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(85px, 1fr)); gap: 8px; }

.slot-btn { 
  padding: 10px 6px; 
  border: 1.5px solid var(--border-default); 
  border-radius: 6px; 
  background: var(--bg-card); 
  color: var(--text-primary); 
  cursor: pointer; 
  font-size: 0.8rem; 
  font-weight: 800; 
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1); 
  text-align: center;
}

.slot-btn:hover { 
  border-color: var(--color-primary); 
  color: var(--color-primary); 
  background: var(--bg-input); 
  transform: translateY(-2px);
  box-shadow: 0 4px 12px var(--color-primary-glow);
}

.no-slots { text-align: center; color: var(--text-tertiary); padding: 60px 20px; font-style: italic; }

.see-more-container {
  margin-top: 20px;
  text-align: center;
  border-top: 1px dashed var(--border-default);
  padding-top: 25px;
}

.see-more-btn {
  background: none;
  border: none;
  color: var(--color-primary);
  padding: 10px 24px;
  border-radius: 0;
  font-weight: 800;
  font-size: 0.9rem;
  cursor: pointer;
  transition: all 0.2s;
}

.see-more-btn:hover {
  text-decoration: underline;
}

/* Custom Scrollbar */
.modal-content::-webkit-scrollbar { width: 6px; }
.modal-content::-webkit-scrollbar-track { background: transparent; }
.modal-content::-webkit-scrollbar-thumb { background: var(--color-gray-300); border-radius: 0; }
.modal-content::-webkit-scrollbar-thumb:hover { background: var(--text-tertiary); }
</style>
