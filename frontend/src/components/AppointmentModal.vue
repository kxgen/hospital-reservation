<script setup>
import { ref, computed} from 'vue';

const props = defineProps({
  doctor: Object,
  visible: Boolean,
  appointments: Object
});

const emit = defineEmits(['close', 'book']);

// Example specialties (you can populate dynamically)
const specialties = ref(['General Checkup', 'Feeling Uncomfort', '...', '...']);
const selectedSpecialty = ref(specialties.value[0]);

const daysWithSlots = computed(() => {
  return Object.entries(props.appointments || {}).map(([dateStr, slots]) => {
    return { date: new Date(dateStr), slots };
  });
});

const formatDate = (date) =>
  date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });

const selectSlot = (date, slot) => {
  emit('book', { date, slot });
};
</script>

<template>
  <div v-if="props.visible" class="modal-overlay" @click.self="emit('close')">
    <div class="modal-content">
      <div class="modal-header">
        <h2>Book an appointment</h2>
        <button class="close-btn" @click="emit('close')">&times;</button>
      </div>

      <div class="doctor-info">
        <div class="photo">Dr</div>
        <div class="details">
          <h3>{{ doctor.name }}</h3>
          <p>{{ doctor.name }}, MD</p>
          <p>{{ doctor.specialty }}</p>
        </div>
      </div>

      <div class="specialty-select">
        <label for="specialty">Reason</label>
        <select id="specialty" v-model="selectedSpecialty">
          <option v-for="s in specialties" :key="s">{{ s }}</option>
        </select>
      </div>

      <div class="appointments">
        <h4>Available Appointments</h4>
        <div class="slots">
          <div
            v-for="day in daysWithSlots"
            :key="day.date.toISOString()"
            class="day-slot"
          >
            <div class="day-label">{{ formatDate(day.date) }}</div>
            <div class="slots-grid">
              <button
                v-for="slot in day.slots"
                :key="slot"
                class="slot-btn"
                @click="selectSlot(day.date, slot)"
              >
                {{ slot }}
              </button>
              <span v-if="day.slots.length === 0" class="no-slots">No available appointments</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0,0,0,0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  overflow-y: auto;
}

.modal-content {
  background: #fff;
  color: #000;
  border-radius: 8px;
  max-width: 600px;
  width: 90%;
  max-height: 90vh;
  overflow-y: auto;
  padding: 20px;
  box-sizing: border-box;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5em;
  cursor: pointer;
}

.doctor-info {
  display: flex;
  gap: 15px;
  margin: 15px 0;
}

.photo {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  background-color: #ccc;
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
  font-weight: bold;
}
.details p {
  margin: 2px 0;
  font-size: 0.9em;
}

.specialty-select {
  margin-bottom: 15px;
}
.specialty-select label {
  display: block;
  margin-bottom: 5px;
  font-weight: bold;
}
.specialty-select select {
  width: 100%;
  padding: 5px;
  font-size: 1em;
}

.appointments h4 {
  margin-bottom: 10px;
}

.day-slot {
  margin-bottom: 15px;
}

.day-label {
  font-weight: bold;
  margin-bottom: 5px;
}

.slots-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.slot-btn {
  padding: 6px 12px;
  border: 1px solid #000;
  border-radius: 4px;
  background: #000;
  color: #fff;
  cursor: pointer;
  font-size: 0.85em;
}
.slot-btn:hover {
  background: #555;
}

.no-slots {
  font-style: italic;
  color: #777;
}
</style>
