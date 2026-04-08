<script setup>
import Header from '@/layouts/Header.vue'
import Footer from '@/layouts/Footer.vue'
import { ref } from 'vue'

const activeFaq = ref(null)
const toggleFaq = (index) => {
  activeFaq.value = activeFaq.value === index ? null : index
}

const faqs = [
  { q: "How do I book an appointment?", a: "Search for a doctor by specialty, select an available time slot, and confirm. You'll receive an instant confirmation via email." },
  { q: "Can I cancel my booking?", a: "Yes, you can cancel or reschedule up to 24 hours before your appointment through your dashboard." },
  { q: "Is there a fee to use the platform?", a: "Our booking service is completely free for patients. You only pay the doctor's consultation fee at the clinic." },
  { q: "Are the doctors verified?", a: "Absolutely. Every doctor in our network undergoes a strict verification of their medical license and credentials." }
]
</script>

<template>
  <div class="support-page">
    <Header />

    <section class="support-hero">
      <div class="container">
        <h1>Support <span>Center</span></h1>
        <p>Everything you need to know about our platform and care process.</p>
      </div>
    </section>

    <section class="contact-strip">
      <div class="container contact-flex">
        <div class="c-card">
          <span class="c-icon">📞</span>
          <div class="c-info">
            <h4>Call Us</h4>
            <p>+251 900 000 000</p>
          </div>
        </div>
        <div class="c-card">
          <span class="c-icon">✉️</span>
          <div class="c-info">
            <h4>Email</h4>
            <p>support@healthcare.com</p>
          </div>
        </div>
        <div class="c-card">
          <span class="c-icon">🕒</span>
          <div class="c-info">
            <h4>Availability</h4>
            <p>24/7 Service</p>
          </div>
        </div>
      </div>
    </section>

    <section class="faq-section">
      <div class="container narrow">
        <h2 class="section-title">Common Questions</h2>
        
        <div class="faq-list">
          <div 
            v-for="(faq, index) in faqs" 
            :key="index" 
            class="faq-card"
            :class="{ active: activeFaq === index }"
            @click="toggleFaq(index)"
          >
            <div class="faq-header">
              <h3>{{ faq.q }}</h3>
              <span class="arrow">↓</span>
            </div>
            <div class="faq-body" v-if="activeFaq === index">
              <p>{{ faq.a }}</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section class="support-footer">
      <div class="container">
        <div class="cta-inner">
          <h3>Still need help?</h3>
          <p>Our team is ready to assist you with any technical or medical booking issues.</p>
          <router-link to="/contact" class="btn-primary">Message Us Directly</router-link>
        </div>
      </div>
    </section>

    <Footer />
  </div>
</template>

<style scoped>
.support-page {
  background: var(--bg-card);
}

.container {
  width: 90%;
  max-width: 1100px;
  margin: 0 auto;
}

.container.narrow {
  max-width: 800px;
}

/* === Hero === */
.support-hero {
  padding: 100px 0 60px;
  background: linear-gradient(to bottom, var(--bg-input), var(--bg-card));
  text-align: center;
}

.support-hero h1 {
  font-size: 3.5rem;
  font-weight: 800;
  color: var(--text-primary);
  margin-bottom: 15px;
}

.support-hero h1 span {
  color: var(--color-primary);
}

.support-hero p {
  font-size: 1.2rem;
  color: var(--text-secondary);
}

.contact-strip {
  margin-top: -30px; /* Pulls cards up into the hero space */
  margin-bottom: 60px;
}

.contact-flex {
  display: flex;
  justify-content: center;
  gap: 20px;
  flex-wrap: wrap;
}

.c-card {
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  padding: 20px 30px;
  border-radius: 0;
  display: flex;
  align-items: center;
  gap: 15px;
  box-shadow: var(--shadow-sm);
  min-width: 280px;
}

.c-icon {
  font-size: 1.5rem;
  background: var(--bg-input);
  width: 50px;
  height: 50px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
}

.c-info h4 {
  margin: 0;
  color: var(--text-primary);
  font-size: 1rem;
}

.c-info p {
  margin: 0;
  color: var(--color-primary);
  font-weight: 600;
}

/* === FAQ Section === */
.faq-section {
  padding: 60px 0;
  background: var(--bg-body);
}

.section-title {
  text-align: center;
  margin-bottom: 40px;
  font-size: 2rem;
}

.faq-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.faq-card {
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  border-radius: 0;
  padding: 20px 25px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.faq-card:hover {
  border-color: var(--color-primary);
}

.faq-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.faq-header h3 {
  font-size: 1.1rem;
  margin: 0;
  color: var(--text-primary);
}

.arrow {
  color: var(--color-primary);
  font-weight: bold;
  transition: transform 0.3s;
}

.faq-card.active .arrow {
  transform: rotate(180deg);
}

.faq-body {
  margin-top: 15px;
  padding-top: 15px;
  border-top: 1px solid var(--border-default);
  color: var(--text-secondary);
  line-height: 1.6;
}

/* === CTA Footer === */
.support-footer {
  padding: 100px 0;
  text-align: center;
}

.cta-inner {
  background: var(--color-primary);
  padding: 60px;
  border-radius: 20px;
  color: var(--color-white);
}

.cta-inner h3 {
  font-size: 2.2rem;
  margin-bottom: 10px;
}

.cta-inner p {
  margin-bottom: 30px;
  opacity: 0.9;
}

.cta-inner .btn-primary {
  background: var(--color-white);
  color: var(--color-primary);
  box-shadow: var(--shadow-md);
}

.cta-inner .btn-primary:hover {
  background: var(--bg-input);
  border-color: var(--color-white);
}

/* === Shared Button Styles === */
.btn-primary {
  display: inline-block;
  padding: 16px 35px;
  text-decoration: none;
  border-radius: 8px;
  font-weight: 700;
  transition: all 0.3s;
}

@media (max-width: 768px) {
  .support-hero h1 { font-size: 2.5rem; }
  .c-card { width: 100%; }
  .cta-inner { padding: 40px 20px; }
}
</style>