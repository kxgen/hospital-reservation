<script setup>
import { ref, computed } from 'vue';
import TimeSlotModal from './TimeSlotModal.vue';
import { useRouter } from 'vue-router'; // Add this import at the top
import { formatTime } from '@/utils/dateFormatter';
const router = useRouter();


const props = defineProps({
    doctor: Object,
    weekDates: Array 
});

const todayStart = new Date();
todayStart.setHours(0, 0, 0, 0);

const dates = computed(() => props.weekDates || []);

const formatLocalDateKey = (date) => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
};

const availableAppointments = computed(() => {
    if (!props.doctor.timeslots) return {};
    const slotsByDate = {};
    const now = new Date();
    props.doctor.timeslots.forEach(slot => {
        const rawStartTime = slot.StartTime || slot.startTime;
        const rawIsAvailable = slot.IsAvailable !== undefined ? slot.IsAvailable : slot.isAvailable;
        if (!rawStartTime) return;
        const slotDateObj = new Date(rawStartTime);
        if (slotDateObj < now) return;
        if (rawIsAvailable === true) {
            const dateString = formatLocalDateKey(slotDateObj);
            const slotTime = formatTime(slotDateObj);
            if (!slotsByDate[dateString]) slotsByDate[dateString] = [];
            slotsByDate[dateString].push(slotTime);
        }
    });
    return slotsByDate;
});

const formatDayName = (date) => date.toLocaleDateString("en-US", { weekday: "short" });
const formatDayNumber = (date) => date.getDate();

const showModal = ref(false);
const selectedDate = ref(null);

const openModal = (date) => {
    const dateString = formatLocalDateKey(date);
    if (availableAppointments.value[dateString]?.length > 0) {
        selectedDate.value = date;
        showModal.value = true;
    }
};

const viewProfile = () => {
  if (props.doctor && props.doctor.id) {
    router.push({ 
      name: 'doctor-overview', 
      params: { id: props.doctor.id.toString() } // Converting to string often fixes "invalid param" warnings
    });
  } else {
    console.error("Doctor ID is missing", props.doctor);
  }
};

const closeModal = () => { showModal.value = false; };
</script>

<template>
  <div class="doc-card">
    <div class="doc-identity">
      <div class="doc-avatar">
        <span>{{ doctor.name?.split(' ').map(n => n[0]).join('').replace('Dr.', '') || 'DR' }}</span>
      </div>
      <div class="doc-text">
        <h3 class="name">{{ doctor.name }}</h3>
        <p class="specialty">{{ doctor.specialty }}</p>
        <button 
          class="profile-btn" 
          @click="viewProfile"
        >
          View Profile
        </button>
      </div>
    </div>

    <div class="doc-schedule">
      <div class="dates-grid">
        <button
          v-for="date in dates"
          :key="date.toISOString()"
          class="date-cell"
          :class="{
            'available': availableAppointments[formatLocalDateKey(date)]?.length > 0
          }"
          @click="openModal(date)"
        >
          <span class="day-name">{{ formatDayName(date) }}</span>
          <span class="day-num">{{ formatDayNumber(date) }}</span>
          <span v-if="availableAppointments[formatLocalDateKey(date)]?.length" class="slot-indicator">
            {{ availableAppointments[formatLocalDateKey(date)].length }}
          </span>
        </button>
      </div>
    </div>

    <TimeSlotModal
      v-if="showModal"
      :doctor="doctor"
      :date="selectedDate"
      :appointments="doctor.timeslots" 
      :visible="showModal"
      @close="closeModal"
    />
  </div>
</template>

<style scoped>
.doc-card {
  display: flex;
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  border-radius: 0;
  padding: 24px;
  margin-bottom: 16px;
  gap: 30px;
  align-items: center;
  max-width: 100%;
  overflow: hidden;
  transition: 0.2s ease-in;
}

.doc-card:hover {
  box-shadow: var(--shadow-md);
  border-color: var(--card-border);
}

/* Identity Section */
.doc-identity {
  display: flex;
  align-items: center;
  gap: 20px;
  min-width: 0;
  flex: 0 0 auto;
}

.doc-avatar {
  width: 90px;
  height: 90px;
  padding: 10px;
  background: var(--bg-input);
  color: var(--color-primary-hover);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.8rem;
  font-weight: 800;
  border: 2px solid var(--border-default);
  flex-shrink: 0;
}

.doc-text {
  min-width: 0;
}

.doc-text .name { 
  margin: 0; 
  font-size: 1.2rem; 
  color: var(--text-primary); 
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.doc-text .specialty { 
  margin: 4px 0 12px 0; 
  color: var(--color-primary); 
  font-weight: 600; 
  font-size: 0.95rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.profile-btn { 
  background: var(--bg-card); 
  border: 1px solid var(--border-default); 
  padding: 6px 14px; 
  border-radius: 8px; 
  font-size: 0.8rem; 
  cursor: pointer; 
  color: var(--text-secondary);
  white-space: nowrap;
}

.profile-btn:hover { 
  background: var(--bg-input);
}
/* Schedule Section - Grid Layout */
.doc-schedule {
  flex: 1 1 auto;
  min-width: 0;
}

.dates-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr); /* 7 days per row */
  gap: 8px;
  width: 100%;
}

.date-cell {
  aspect-ratio: 1 / 1; /* Make cells square */
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: var(--bg-card);
  border: 1.5px solid var(--border-default);
  border-radius: 0;
  cursor: pointer;
  transition: all 0.2s ease;
  padding: 4px;
  position: relative;
  min-height: 0; /* Allow shrinking */
}

/* Available dates */
.date-cell.available {
  background: var(--bg-input);
  border-color: var(--border-default);
}

.date-cell.available:hover {
  background: var(--bg-input);
  border-color: var(--color-primary);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px var(--color-primary-glow);
}

/* Date cell content */
.date-cell .day-name {
  font-size: 0.65rem;
  font-weight: 700;
  color: var(--text-tertiary); /* Muted light gray */
  text-transform: uppercase;
  line-height: 1;
  margin-bottom: 2px;
}

.date-cell .day-num {
  font-size: 1.1rem;
  font-weight: 800;
  color: var(--text-tertiary); /* Very muted light gray for off-dates */
  line-height: 1.2;
}

.date-cell.available .day-name {
  color: var(--text-secondary); /* Normal gray for available days info */
}

.date-cell.available .day-num {
  color: var(--color-primary-hover);
}

.date-cell.past .day-num {
  color: var(--text-tertiary);
}

/* Slot indicator - small circle with number */
.slot-indicator {
  position: absolute;
  top: -5px;
  right: -5px;
  width: 16px;
  height: 16px;
  background: var(--color-primary);
  color: var(--color-white);
  border-radius: 50%;
  font-size: 0.6rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
  padding: 1px;
}

/* Responsive grid adjustments */
@media (max-width: 1200px) {
  .dates-grid {
    gap: 6px;
  }
  
  .date-cell .day-name {
    font-size: 0.6rem;
  }
  
  .date-cell .day-num {
    font-size: 1rem;
  }
  
  .slot-indicator {
    width: 14px;
    height: 14px;
    font-size: 0.55rem;
  }
}

@media (max-width: 992px) {
  .doc-card {
    gap: 20px;
    padding: 20px;
  }
  
  .dates-grid {
    grid-template-columns: repeat(4, 1fr); /* 4 columns on medium screens */
    gap: 8px;
  }
  
  .date-cell {
    aspect-ratio: 1 / 0.8; /* Slightly taller for better readability */
  }
}

@media (max-width: 768px) {
  .doc-card {
    flex-direction: column;
    align-items: flex-start;
    gap: 20px;
  }
  
  .doc-identity {
    width: 100%;
  }
  
  .doc-schedule {
    width: 100%;
  }
  
  .dates-grid {
    grid-template-columns: repeat(7, 1fr); /* Back to 7 columns on tablets */
    gap: 6px;
  }
  
  .date-cell {
    aspect-ratio: 1 / 1;
  }
}

@media (max-width: 576px) {
  .dates-grid {
    grid-template-columns: repeat(4, 1fr); /* 4 columns on small screens */
    gap: 8px;
  }
  
  .date-cell {
    aspect-ratio: 1 / 0.9;
  }
  
  .date-cell .day-name {
    font-size: 0.55rem;
  }
  
  .date-cell .day-num {
    font-size: 0.9rem;
  }
}

@media (max-width: 400px) {
  .dates-grid {
    grid-template-columns: repeat(3, 1fr); /* 3 columns on very small screens */
    gap: 6px;
  }
  
  .doc-avatar {
    width: 70px;
    height: 70px;
    font-size: 1.4rem;
  }
  
  .doc-text .name {
    font-size: 1rem;
  }
}
</style>