<script setup>
import Header from '@/components/Header.vue';
import Footer from '@/components/Footer.vue'
</script>

<template>
  <div>
    <Header />

    <!-- Hero -->
    <section class="hero">
      <div class="hero-inner">
        <h1>Find and Book Your Next <span>Doctor</span> Appointment</h1>
        <p>Many patients use us to find quality healthcare.</p>

        <div class="search-box">
          <input type="text" placeholder="Search Doctor or Specialty" />
          <button>
            <router-link to="/doctor-search">Search</router-link>
          </button>
        </div>

        <p class="help-text">
          Need help? Call us at <a href="tel:_________">_________</a>
        </p>
      </div>
    </section>

    <!-- Stats -->
    <section class="stats">
      <div class="container stats-grid">
        <div>
          <h3>5K+</h3>
          <p>Patients Served</p>
        </div>
        <div>
          <h3>40+</h3>
          <p>Doctors & Specialists</p>
        </div>
        <div>
          <h3>12+</h3>
          <p>Years of Service</p>
        </div>
      </div>
    </section>


    <!-- Features -->
    <section class="features">
      <div class="container">
        <h2>Simple Steps to Better Care</h2>
        <div class="features-grid">
          <div class="feature-item">
            <h3>Find the Right Doctor</h3>
            <p>Search by specialty or condition to find the perfect fit.</p>
          </div>
          <div class="feature-item">
            <h3>Book Instantly</h3>
            <p>See real-time availability and book appointments online 24/7.</p>
          </div>
          <div class="feature-item">
            <h3>Free and Easy</h3>
            <p>Booking appointments is always free, fast, and simple.</p>
          </div>
        </div>
      </div>
    </section>

    <!-- Specialties -->
    <section class="specialties">
      <div class="container">
        <h2>Specialties Include</h2>
        <div class="specialty-grid">
          <div class="specialty-item">Dentist</div>
          <div class="specialty-item">Primary Care</div>
          <div class="specialty-item">Dermatologist</div>
          <div class="specialty-item">Ophthalmologist</div>
          <div class="specialty-item">Psychiatrist</div>
          <div class="specialty-item">Gynecologist</div>
        </div>
        <div class="view-all">
          <a href="#">View All Specialties (50+)</a>
        </div>
      </div>
    </section>

    <Footer/>
  </div>
</template>

<style scoped>
/* Reset and base layout */
body {
  margin: 0;
  font-family: Arial, sans-serif;
}

.container {
  width: 90%;
  max-width: 1100px;
  margin: 0 auto;
}

/* Hero Section */
.hero {
  margin-top: 60px; /* below fixed header */
  padding: 100px 20px 60px;
  text-align: center;
  background: #f9f9f9;
}

.hero h1 {
  font-size: 2em;
  margin-bottom: 10px;
  color: #000;
}

.hero h1 span {
  color: #000;
  font-weight: bold;
}

.hero p {
  margin-bottom: 25px;
  color: #444;
}

.search-box {
  display: flex;
  justify-content: center;
  max-width: 600px;
  margin: 0 auto 15px;
  border: 1px solid #aaa;
}

.search-box input {
  flex: 1;
  padding: 10px;
  border: none;
  outline: none;
}

.search-box button {
  background: #000;
  color: #fff;
  border: none;
  padding: 0 20px;
  cursor: pointer;
}

.search-box button a {
  color: inherit;
  text-decoration: none;
  display: block;
}

.help-text {
  font-size: 0.9em;
  color: #666;
}

/* Stats Section */
.stats {
  padding: 40px 0;
  background: #fff;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  text-align: center;
}

.stats h3 {
  margin-bottom: 5px;
  font-size: 1.4em;
  color: #000;
}

/* Features Section */
.features {
  padding: 60px 20px;
  background: #f3f3f3;
}

.features h2 {
  text-align: center;
  margin-bottom: 40px;
}

.features-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
}

.feature-item {
  background: #fff;
  border: 1px solid #ccc;
  padding: 20px;
  text-align: center;
}

/* Specialties Section */
.specialties {
  padding: 60px 20px;
  background: #fff;
  text-align: center;
}

.specialty-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 15px;
  margin-top: 20px;
}

.specialty-item {
  border: 1px solid #ccc;
  padding: 20px;
  background: #fff;
}

.view-all {
  margin-top: 25px;
}


/* Responsive */
@media (max-width: 768px) {
  .hero {
    padding: 80px 10px 40px;
  }
}
</style>
