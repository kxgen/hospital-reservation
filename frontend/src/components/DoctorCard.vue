<script setup>
import { ref, computed } from 'vue';
import AppointmentModal from './AppointmentModal.vue';

const props = defineProps({
  doctor: Object
});

const today = new Date();
const dates = computed(() => {
  const arr = [];
  for (let i = 0; i < 14; i++) {
    const d = new Date(today);
    d.setDate(today.getDate() + i);
    arr.push(new Date(d));
  }
  return arr;
});

const appointments = {
  "2025-11-22": ["11:30 AM", "2:00 PM"],
  "2025-11-23": ["10:30 AM", "11:00 AM", "11:30 AM"],
  "2025-11-24": ["10:00 AM", "10:30 AM", "11:00 AM", "11:30 AM"],
};

const showModal = ref(false);
const selectedDate = ref(null);

const formatDay = (date) => date.toLocaleDateString("en-US", { weekday: "short" });
const formatDayNumber = (date) => date.getDate();

const openModal = (date) => {
  selectedDate.value = date;
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const bookAppointment = ({ slot, date }) => {
  alert(`You booked ${slot} with ${props.doctor.name} on ${date.toDateString()}`);
  closeModal();
};
</script>

<template>
  <div class="doctor-card">
    <div class="doctor-info">
      <div class="photo">Dr</div>
      <div class="details">
        <h3>{{ doctor.name }}</h3>
        <p class="specialty">{{ doctor.specialty }}</p>
    </div>
  </div>


    <div class="schedule-container">
      <div class="schedule-grid">
        <button
          v-for="date in dates"
          :key="date.toISOString()"
          class="day-box"
          :class="{ available: appointments[date.toISOString().slice(0,10)] }"
          @click="openModal(date)"
        >
          <div class="day">{{ formatDay(date) }}</div>
          <div class="num">{{ formatDayNumber(date) }}</div>

          <div v-if="appointments[date.toISOString().slice(0,10)]?.length" class="slot-info">
            {{ appointments[date.toISOString().slice(0,10)].length }} appt{{ appointments[date.toISOString().slice(0,10)].length > 1 ? 's' : '' }}
          </div>
          <div v-else class="slot-none">No appts</div>
        </button>
      </div>
    </div>

    <AppointmentModal
      :doctor="doctor"
      :date="selectedDate"
      :appointments="appointments"
      :visible="showModal"
      @close="closeModal"
      @book="bookAppointment"
    />
  </div>
</template>

<style scoped>
.doctor-card {
  display: flex;
  justify-content: space-between;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 20px;
  background: #fff;
  gap: 20px;
}

.doctor-info {
  display: flex;
  gap: 15px;
  flex: 1.5;
}

.photo {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  background-color: #ccc; /* grey placeholder */
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  color: #555;
  font-size: 1.2em;
  flex-shrink: 0;
}

.details h3 {
  margin: 0;
  color: #000; /* black instead of teal */
}
.specialty {
  margin-top: 4px;
  color: #444;
}

.schedule-container {
  flex: 1;
}
.schedule-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 8px;
}
.day-box {
  border: 1px solid #ddd;
  border-radius: 6px;
  text-align: center;
  padding: 8px;
  cursor: pointer;
  background: #fff; /* default white background */
  color: #000; /* black text */
  transition: 0.2s;
}
.day-box.available {
  background: #000; /* full black background for available */
  color: #fff; /* white text */
  border-color: #000;
}
.day-box:hover {
  background: #555; /* slightly lighter black on hover */
  color: #fff;
}
.slot-info {
  font-weight: bold;
  margin-top: 4px;
}
.slot-none {
  color: #aaa;
  font-style: italic;
  margin-top: 4px;
}
</style>

