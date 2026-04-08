<script setup>
import Header from '@/layouts/Header.vue'
import Footer from '@/layouts/Footer.vue'
</script>

<template>
  <div>
    <Header />

    <section class="hero">
      <div class="hero-inner container">
        <div class="hero-content">
          <h1>Find & Book Your Next <span>Doctor</span> Appointment, Instantly.</h1>
          <p class="hero-subtitle">
            Skip the phone calls and long waits. Access our network of verified specialists and book
            your visit online, 24/7.
          </p>

          <router-link to="/doctors" class="btn-primary"> Find a Doctor </router-link>

          <p class="help-text">
            Need urgent help? Call our emergency line:
            <a href="tel:+251900000000">+251 900 000 000</a>
          </p>
        </div>
        <div class="hero-image">
          <img src="../../../public/images/hero.jpg" alt="Hero Image" />
        </div>
      </div>
    </section>

    <section class="stats">
      <div class="container stats-grid">
        <div class="stat-item">
          <h3>1K+</h3>
          <p>Patients Served</p>
        </div>
        <div class="stat-item">
          <h3>20+</h3>
          <p>Verified Specialists</p>
        </div>
        <div class="stat-item">
          <h3> > 90%</h3>
          <p>Satisfaction Rate</p>
        </div>
        <div class="stat-item">
          <h3>7+</h3>
          <p>Years of Service</p>
        </div>
      </div>
    </section>

    <section class="features">
      <div class="container">
        <h2 class="section-title">Simple Steps to Better Care</h2>
<div class="features-grid">
  <div class="feature-item">
    <h3>1. Find the Right Doctor</h3>
    <p>
      Browse qualified specialists using filters based on expertise, availability, and your preferred time.
    </p>
  </div>
  <div class="feature-item">
    <h3>2. Book Instantly</h3>
    <p>
      Choose an available time from the doctor’s schedule. No confirmation calls required.
    </p>
  </div>
  <div class="feature-item">
    <h3>3. Get Care</h3>
    <p>
      Receive instant appointment confirmation and timely reminders before your visit.
    </p>
  </div>
</div>

      </div>
    </section>

    <section class="specialties">
      <div class="container">
        <h2 class="section-title">Explore Our Top Specialties</h2>

      <div class="specialty-grid">
        <div class="specialty-item">Dentist</div>
        <div class="specialty-item">Primary Care</div>
        <div class="specialty-item">Dermatologist</div>
        <div class="specialty-item">Ophthalmologist</div>
        <div class="specialty-item">Psychiatrist</div>
        <div class="specialty-item">Gynecologist</div>
        <div class="specialty-item">Cardiologist</div>
        <div class="specialty-item">Pediatrician</div>
      </div>

        <div class="view-all">
          <router-link to="/doctors" class="btn-secondary"
            >View All Doctors & Specialties</router-link
          >
        </div>
      </div>
    </section>

    <Footer />
  </div>
</template>

<style scoped>
/* Theme Color: #00b39e (Teal/Turquoise) */

/* Base & Typography */
.container {
  width: 90%;
  max-width: 1200px;
  margin: 0 auto;
}
.section-title {
  text-align: center;
  font-size: 2.2rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 50px;
}

/* === Hero Section === */
.hero {
  /* Offset by header height (70px) */
  padding: 100px 0 0 0; /* Removed bottom padding so image can sit at base */
  overflow: hidden;
  background-color: var(--bg-body);
}

.hero-inner {
  display: flex;
  align-items: center; /* Content stays centered */
  gap: 50px;
  text-align: left;
  min-height: 500px;
}

.hero-content {
  flex: 1;
}

.hero h1 {
  font-size: 3.2rem;
  line-height: 1.2;
  margin-bottom: 20px;
  color: var(--text-primary);
  font-weight: 800;
}

.hero h1 span {
  color: var(--color-primary); /* Theme highlight */
  font-weight: 800;
}

.hero-subtitle {
  font-size: 1.2rem;
  color: var(--text-secondary);
  margin-bottom: 30px;
}

.hero-image {
  align-self: flex-end; /* Sit on the bottom of the section */
  flex: 3; /* Increased for more presence */
  min-height: 480px;
  display: flex;
  align-items: flex-end;
  justify-content: center;
  padding: 0;
  margin-top: 50px;
  overflow: hidden;

}

.hero-image img {
  border-radius: 15%;
  width: 100%;
  height: auto;
  max-height: 750px;
  object-fit: contain;
  display: block;

mask-image: 
    linear-gradient(to bottom, transparent, black 8%, black 85%, transparent),
    linear-gradient(to right, transparent, black 10%, black 85%, transparent);
  
  /* This ensures the gradients work together rather than overlapping weirdly */
  mask-composite: intersect;
  -webkit-mask-composite: source-in; /* For Safari support */
}

.btn-primary {
  display: inline-block;
  padding: 18px 40px;
  background: var(--color-primary);
  color: var(--color-white);
  text-decoration: none;
  border-radius: 8px;
  font-weight: 700;
  font-size: 1.25rem;
  margin-bottom: 25px;

  /* Added a 2px border here so it doesn't "jump" when it lights up on hover */
  border: 2px solid transparent;

  /* Updated transition to include 'all' for smoother border/shadow blending */
  transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
  box-shadow: var(--shadow-primary);
}

.btn-primary:hover {
  background: var(--text-link-hover);
  transform: translateY(-3px);

  /* The "Light Up" - Lighter border + a subtle outer glow */
  border-color: var(--color-primary-light);
  box-shadow: 0 12px 25px var(--color-primary-glow), 0 0 10px var(--color-primary-glow);
}

.btn-primary:active {
  transform: translateY(-1px); /* Tactile feel when clicked */
}

.help-text {
  font-size: 0.95em;
  color: var(--text-tertiary);
}
.help-text a {
  color: var(--color-primary);
  font-weight: 600;
  text-decoration: none;
}
.help-text a:hover {
  text-decoration: underline;
}

/* === Stats Section === */
.stats {
  padding: 50px 0;
  background: var(--bg-card);
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 30px;
  text-align: center;
}

.stat-item h3 {
  margin-bottom: 5px;
  font-size: 2.5em; /* Large numbers */
  color: var(--color-primary); /* Theme color for numbers */
  font-weight: 800;
}
.stat-item p {
  color: var(--text-secondary);
  font-size: 1.1rem;
}

/* === Features Section (How It Works) === */
.features {
  padding: 90px 0;
  background: linear-gradient(
    180deg,
    var(--color-gray-50),
    var(--color-gray-100)
  );
}

.features-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 32px;
}

/* Card */
.feature-item {
  position: relative;
  background: var(--bg-card);
  border-radius: 0;
  padding: 36px 28px;
  text-align: center;
  box-shadow: var(--shadow-sm);
  transition: transform 0.2s ease, box-shadow 0.35s ease;
  overflow: hidden;
}

/* Accent bar */
.feature-item::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  height: 5px;
  width: 100%;

}

/* Hover effect */
.feature-item:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
}

/* Icon */
.feature-item .icon {
  font-size: 2.8rem;
  margin-bottom: 18px;
  color: var(--color-primary);
}

/* Title */
.feature-item h3 {
  font-size: 1.35rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 12px;
}

/* Description */
.feature-item p {
  font-size: 0.95rem;
  line-height: 1.6;
  color: var(--text-secondary);
}

/* Optional subtle number emphasis */
.feature-item h3::first-letter {
  color: var(--color-primary);
}


/* === Specialties Section === */
.specialties {
  padding: 80px 0;
  background: var(--bg-card);
}

.specialty-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  margin-top: 30px;
}

.specialty-item {
  border: 1px solid var(--border-default);
  padding: 20px;
  background: var(--bg-card);
  text-decoration: none;
  color: var(--text-primary);
  font-size: 1.1rem;
  border-radius: 0;
  transition:
    background 0.2s,
    border-color 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
}
.specialty-item:hover {
  background: var(--color-primary-light); /* Light theme background */
  border-color: var(--color-primary);
  color: var(--color-primary);
}

/* View All Button (Secondary Action) */
.view-all {
  margin-top: 40px;
  text-align: center;
}
.btn-secondary {
  display: inline-block;
  padding: 10px 20px;
  background: none;
  color: var(--color-primary);
  border: 2px solid var(--color-primary);
  text-decoration: none;
  font-weight: 600;
  border-radius: 6px;
  transition:
    background 0.2s,
    color 0.2s;
}
.btn-secondary:hover {
  background: var(--color-primary);
  color: var(--color-white);
}

/* === Responsive Adjustments === */
@media (max-width: 1024px) {
  .hero h1 {
    font-size: 2.5rem;
  }
}

@media (max-width: 768px) {
  .hero {
    padding: 200px 0 0 0;
  }
  .hero-inner {
    flex-direction: column;
    text-align: center;
  }
  .hero-image {
    min-height: 350px;
    order: -1; /* Place image above text on mobile */
    width: 100%;
    margin-bottom: 20px;
    border-radius: 0;
  }


  .hero h1 {
    font-size: 2rem;
  }
  .stats-grid {
    grid-template-columns: repeat(2, 1fr); /* 2x2 grid */
  }
  .features-grid {
    grid-template-columns: 1fr; /* Stack features */
  }
  .section-title {
    font-size: 1.8rem;
  }
  .features,
  .specialties {
    padding: 50px 0;
  }
  /* Mobile CTA adjustment */
  .btn-primary {
    padding: 15px 30px;
    font-size: 1.15rem;
    width: 80%; /* Takes up more horizontal space */
    max-width: 350px;
    display: block; /* Make it block for better centering */
    margin: 0 auto 20px auto;
  }
}
</style>
