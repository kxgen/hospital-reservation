<script setup>
import Header from '@/components/Header.vue';
import Footer from '@/components/Footer.vue';
</script>

<template>
  <div>
    <Header />

    <!-- Hero Section -->
    <section class="hero">
      <div class="hero-inner">
        <h1>Book Your Next <span>Doctor</span> Appointment</h1>
        <p>Many patients rely on us to find quality healthcare quickly and easily.</p>

        <router-link to="/doctors" class="btn-primary">
          Find a Doctor
        </router-link>

        <p class="help-text">
          Need help? Call us at <a href="tel:_________">_________</a>
        </p>
      </div>
    </section>

    <!-- Stats Section -->
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

    <!-- Features Section -->
    <section class="features">
      <div class="container">
        <h2>Simple Steps to Better Care</h2>
        <div class="features-grid">
          <div class="feature-item">
            <h3>Find the Right Doctor</h3>
            <p>Browse doctors by specialty or experience to find the perfect fit.</p>
          </div>
          <div class="feature-item">
            <h3>Book Instantly</h3>
            <p>See real-time availability and book appointments online 24/7.</p>
          </div>
          <div class="feature-item">
            <h3>Free and Easy</h3>
            <p>Booking appointments is always fast, free, and hassle-free.</p>
          </div>
        </div>
      </div>
    </section>

    <!-- Specialties Section -->
    <section class="specialties">
      <div class="container">
        <h2>Our Specialties</h2>
        <div class="specialty-grid">
          <router-link to="/doctors?specialty=dentist" class="specialty-item">Dentist</router-link>
          <router-link to="/doctors?specialty=primary-care" class="specialty-item">Primary Care</router-link>
          <router-link to="/doctors?specialty=dermatologist" class="specialty-item">Dermatologist</router-link>
          <router-link to="/doctors?specialty=ophthalmologist" class="specialty-item">Ophthalmologist</router-link>
          <router-link to="/doctors?specialty=psychiatrist" class="specialty-item">Psychiatrist</router-link>
          <router-link to="/doctors?specialty=gynecologist" class="specialty-item">Gynecologist</router-link>
        </div>
        <div class="view-all">
          <router-link to="/doctors">View All Doctors & Specialties</router-link>
        </div>
      </div>
    </section>

    <Footer />
  </div>
</template>

<style scoped>
/* Base reset */
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
  margin-top: 60px;
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

/* Hero Button */
.btn-primary {
  display: inline-block;
  padding: 12px 25px;
  background: #000;
  color: #fff;
  text-decoration: none;
  border-radius: 4px;
  margin-bottom: 15px;
}
.btn-primary:hover {
  background: #222;
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
  text-decoration: none;
  color: #000;
  display: flex;
  align-items: center;
  justify-content: center;
}
.specialty-item:hover {
  background: #f3f3f3;
}

/* View All */
.view-all {
  margin-top: 25px;
}
.view-all a {
  text-decoration: none;
  color: #000;
  font-weight: bold;
}
.view-all a:hover {
  text-decoration: underline;
}

/* Responsive */
@media (max-width: 768px) {
  .hero {
    padding: 80px 10px 40px;
  }
  .features-grid, .specialty-grid {
    grid-template-columns: 1fr;
  }
}
</style>
