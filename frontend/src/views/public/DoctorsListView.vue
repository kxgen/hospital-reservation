<script setup>
import Header from '@/components/Header.vue';
import DoctorCard from '@/components/DoctorCard.vue';
import { ref, computed } from 'vue';

// --- Weekly schedule state ---
const today = new Date();
const currentWeekStart = ref(getStartOfWeek(today));

function getStartOfWeek(date) {
  const d = new Date(date);
  const day = d.getDay(); // Sunday = 0
  d.setDate(d.getDate() - day);
  return d;
}

const nextWeek = () => {
  const newDate = new Date(currentWeekStart.value);
  newDate.setDate(newDate.getDate() + 7);
  currentWeekStart.value = newDate;
};

const prevWeek = () => {
  const newDate = new Date(currentWeekStart.value);
  newDate.setDate(newDate.getDate() - 7);
  currentWeekStart.value = newDate;
};

const weekRange = computed(() => {
  const start = currentWeekStart.value;
  const end = new Date(start);
  end.setDate(start.getDate() + 6);
  const options = { month: "short", day: "numeric" };
  return `${start.toLocaleDateString("en-US", options)} - ${end.toLocaleDateString("en-US", options)}`;
});

// --- Doctors list ---
const selectedSpecialty = ref("Dermatologists");
const doctors = ref([
  {
    id: 1,
    name: 'Dr. Samuel',
    specialty: 'Dermatologist',
    img: 'local-dummy.png',
  },
  {
    id: 2,
    name: 'Dr. Abebe',
    specialty: 'Family Medicine',
    img: 'local-dummy.png',
  },
  {
    id: 3,
    name: 'Dr. Kebede',
    specialty: 'Pediatrics',
    img: 'local-dummy.png',
  }
]);
</script>

<template>
  <Header />

  <div class="doctor-page">
    
    <!-- Search Box -->
    <header class="search-header">
      <div class="search-input-group">
        <input
          type="text"
          placeholder="Specialty, condition, or doctor..."
          class="search-input"
        />
        <button class="search-btn">Search</button>
      </div>
    </header>

    <!-- Full-width Week Bar -->
    <div class="week-bar">
      <button class="week-btn" @click="prevWeek">‹ Previous</button>
      <div class="week-text">{{ weekRange }}</div>
      <button class="week-btn" @click="nextWeek">Next ›</button>
    </div>

    <!-- MAIN CONTENT -->
    <div class="content-container">

      <!-- Sidebar Filters -->
      <aside class="sidebar">
        <h3>Filters</h3>

        <div class="filter-section">
          <h4>Specialty</h4>
          <ul class="filter-list">
            <li>
              <label>
                <input type="checkbox" name="specialty" value="dermatology" />
                Dermatologist
              </label>
            </li>
            <li>
              <label>
                <input type="checkbox" name="specialty" value="pediatrics" />
                Pediatrics
              </label>
            </li>
            <li>
              <label>
                <input type="checkbox" name="specialty" value="family_medicine" />
                Family Medicine
              </label>
            </li>
          </ul>
        </div>

        <div class="filter-section gender-filter">
          <h4>Gender (Preference)</h4>
          <label><input type="radio" name="gender" value="female" /> Female</label>
          <label><input type="radio" name="gender" value="male" /> Male</label>
        </div>
      </aside>

      <!-- Doctor List -->
      <main class="doctor-list">
        <h2>{{ doctors.length }} available {{ selectedSpecialty }}</h2>

        <DoctorCard
          v-for="doctor in doctors"
          :key="doctor.id"
          :doctor="doctor"
        />
      </main>
    </div>
  </div>
</template>

<style scoped>
/* Page Layout */
.doctor-page {
  display: flex;
  flex-direction: column;
  background: white;
  min-height: 100vh;
  padding-top: 60px;
}

/* Search Header */
.search-header {
  display: flex;
  justify-content: center;
  padding: 20px 0;
  border-bottom: 1px solid #ccc;
}

.search-input-group {
  display: flex;
  width: 90%;
  max-width: 800px;
}

.search-input {
  flex: 1;
  padding: 12px;
  border: 1px solid #aaa;
  border-right: none;
  font-size: 1rem;
}

.search-btn {
  padding: 12px 22px;
  border: 1px solid #aaa;
  cursor: pointer;
}

/* Week Bar */
.week-bar {
  width: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 16px 0;
  border-bottom: 1px solid #ccc;
  gap: 15px;
}

.week-text {
  font-size: 1rem;
  padding: 6px 14px;
  border-radius: 6px;
  border: 1px solid #ccc;
}

.week-btn {
  background: none;
  padding: 6px 12px;
  border: 1px solid #ccc;
  border-radius: 4px;
  cursor: pointer;
}
.week-btn:hover {
  border-color: #00b39e;
  color: #00b39e;
}

/* Main Content Layout */
.content-container {
  display: flex;
  width: 100%;
  max-width: 1200px;
  margin: 30px auto;
  padding: 0 25px;
  gap: 40px;
  align-items: flex-start;
}

/* Sidebar */
.sidebar {
  width: 260px;
  flex-shrink: 0;
  border-right: 1px solid #ccc;
  padding-right: 25px;
}

.sidebar h3 {
  margin-bottom: 15px;
}

.sidebar h4 {
  margin-bottom: 8px;
  font-size: 0.95rem;
}

/* Sidebar lists */
.filter-list {
  list-style: none;
  padding: 0;
  margin-bottom: 20px;
}

.filter-list li {
  margin-bottom: 10px;
}

.gender-filter label {
  display: block;
  margin-bottom: 8px;
}

.doctor-list {
  flex: 1;
  min-width: 0;
}

.doctor-list h2 {
  margin-bottom: 25px;
  font-size: 1.25rem;
}

</style>
