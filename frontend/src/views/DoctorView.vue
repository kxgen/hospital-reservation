<!-- src/pages/DoctorProfilePage.vue -->
<script setup>
import Header from '@/components/Header.vue'

const doctor = {
  id: 1,
  name: 'Dr. Helina Samuel',
  specialty: 'Dermatologist',
  about: `Dr. Helina is a board-certified dermatologist with over a decade of experience treating acne, eczema, and aesthetic skin procedures. She’s known for combining precise clinical care with a warm bedside manner.`,
  img: '/local-dummy.png',
  education: 'MD, Addis Ababa University',
  experience: '10+ years in clinical dermatology',
  languages: ['Amharic', 'English'],
  schedules: {
    'Today (Nov 7)': [
      '9:00 AM', '9:30 AM', '10:00 AM', '10:30 AM',
      '11:00 AM', '1:00 PM', '1:30 PM', '2:00 PM', '3:00 PM', '4:00 PM'
    ],
    'Tomorrow (Nov 8)': [
      '8:30 AM', '9:00 AM', '9:45 AM', '10:15 AM',
      '11:00 AM', '12:00 PM', '1:15 PM', '2:00 PM', '3:30 PM', '4:15 PM', '5:00 PM'
    ],
    'Sat, Nov 9': [
      '9:00 AM', '9:30 AM', '10:15 AM', '11:45 AM',
      '1:00 PM', '1:45 PM', '2:30 PM', '3:15 PM', '4:00 PM'
    ],
    'Sun, Nov 10': ['Closed'],
    'Mon, Nov 11': [
      '8:00 AM', '8:30 AM', '9:15 AM', '9:45 AM', '10:30 AM', '11:15 AM',
      '12:30 PM', '1:30 PM', '2:15 PM', '3:00 PM', '4:00 PM', '5:00 PM'
    ]
  }
}
</script>

<template>
  <Header />

  <div class="doctor-page">
    <!-- Search Header -->
    <header class="search-header">
      <div class="search-input-group">
        <input
          type="text"
          placeholder="Search another doctor or specialty..."
          class="search-input"
        />
        <button class="search-btn">Search</button>
      </div>
    </header>

    <!-- Main Layout -->
    <div class="profile-container">
      <!-- Left: Doctor Info -->
      <section class="doctor-info">
        <img :src="doctor.img" alt="Doctor photo" class="doctor-photo" />

        <div class="doctor-details">
          <h1>{{ doctor.name }}</h1>
          <p class="specialty">{{ doctor.specialty }}</p>
          <p class="about">{{ doctor.about }}</p>

          <div class="extra-info">
            <p><strong>Education:</strong> {{ doctor.education }}</p>
            <p><strong>Experience:</strong> {{ doctor.experience }}</p>
            <p><strong>Languages:</strong> {{ doctor.languages.join(', ') }}</p>
          </div>
        </div>
      </section>

      <!-- Right: Continuous Schedule -->
      <aside class="doctor-schedule">
        <h2>Available Appointments</h2>

        <div class="schedule-list">
          <div
            v-for="(slots, day) in doctor.schedules"
            :key="day"
            class="day-schedule"
          >
            <h3 class="day-label">{{ day }}</h3>

            <div v-if="slots[0] !== 'Closed'" class="time-slots">
              <div v-for="slot in slots" :key="slot" class="slot">
                {{ slot }}
              </div>
            </div>

            <div v-else class="closed-message">
              Doctor not available.
            </div>
          </div>
        </div>

        <button class="book-btn">Book Appointment</button>
      </aside>
    </div>
  </div>
</template>

<style scoped>
/* Container */
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
  padding: 20px;
  border-bottom: 1px solid #ccc;
}

.search-input-group {
  display: flex;
  width: 100%;
  max-width: 900px;
}

.search-input {
  flex: 1;
  padding: 10px;
  border: 1px solid #aaa;
  border-right: none;
  font-size: 1em;
}

.search-btn {
  padding: 10px 20px;
  border: 1px solid #aaa;
  background: #f5f5f5;
  cursor: pointer;
}

/* Layout */
.profile-container {
  display: flex;
  max-width: 1200px;
  margin: 40px auto;
  padding: 0 20px;
  gap: 40px;
}

/* Doctor Info */
.doctor-info {
  display: flex;
  flex: 2;
  gap: 30px;
}

.doctor-photo {
  width: 220px;
  height: 220px;
  object-fit: cover;
  border-radius: 50%;
  border: 2px solid #ddd;
}

.doctor-details {
  flex: 1;
}

.doctor-details h1 {
  font-size: 1.8em;
  margin-bottom: 5px;
}

.specialty {
  font-weight: 500;
  color: #444;
  margin-bottom: 10px;
}

.rating {
  color: #f39c12;
  font-weight: bold;
  margin-bottom: 5px;
}

.address {
  color: #666;
  margin-bottom: 15px;
}

.about {
  margin-bottom: 20px;
  line-height: 1.6;
  color: #333;
}

.extra-info p {
  margin: 5px 0;
}

/* Schedule Section */
.doctor-schedule {
  flex: 1;
  border-left: 1px solid #ccc;
  padding-left: 20px;
  overflow-y: auto;
  max-height: calc(100vh - 180px);
}

.doctor-schedule h2 {
  margin-bottom: 15px;
  font-size: 1.2em;
}

/* Vertical List of Days */
.schedule-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.day-schedule {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 15px;
  background: #fafafa;
}

.day-label {
  font-size: 1em;
  font-weight: bold;
  color: #333;
  margin-bottom: 10px;
}

/* Slots */
.time-slots {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.slot {
  padding: 8px 12px;
  border: 1px solid #aaa;
  border-radius: 6px;
  background: #fdfdfd;
  cursor: pointer;
  font-size: 0.9em;
  transition: all 0.2s;
}

.slot:hover {
  background: #e6f7ff;
  border-color: #3498db;
}

/* Closed Days */
.closed-message {
  color: #888;
  font-style: italic;
  padding: 10px 0;
}

/* Book Button */
.book-btn {
  width: 100%;
  padding: 12px;
  background: #3498db;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 1em;
  cursor: pointer;
  margin-top: 25px;
}

.book-btn:hover {
  background: #2980b9;
}
</style>
