<script setup>
import Header from '@/components/Header.vue';
import { ref } from 'vue';

// No longer strictly needed, but kept for future expansion if account creation becomes an option
const showAccountFields = ref(false); 
const toggleAccountFields = () => {
  showAccountFields.value = !showAccountFields.value;
};

// Dummy selected doctor/time – in a real app, you'd pass this via route params or store
const selectedDoctor = {
  name: 'Dr. Emily Johnson',
  specialty: 'Dermatologist',
  time: '10:00 AM, Oct 30, 2025',
};

// State for the new fields
const visitReason = ref('');
const insuranceConfirmed = ref(false);
const policyAcknowledged = ref(false);

const submitBooking = () => {
    // In a real application, you would send this data to an API endpoint
    if (visitReason.value && insuranceConfirmed.value && policyAcknowledged.value) {
        console.log('Booking submitted with data:', {
            doctor: selectedDoctor.name,
            time: selectedDoctor.time,
            reason: visitReason.value,
            insurance: insuranceConfirmed.value,
            policy: policyAcknowledged.value,
        });
        alert('Appointment successfully booked!');
    } else {
        alert('Please fill in the reason for the visit and confirm policies.');
    }
}
</script>

<template>
  <Header />

  <main class="booking-page">
    <div class="container">
      <h2>Complete Your Reservation</h2>

      <section class="summary">
        <h3>Appointment Details</h3>
        <p><strong>Doctor:</strong> {{ selectedDoctor.name }} ({{ selectedDoctor.specialty }})</p>
        <p><strong>Time:</strong> {{ selectedDoctor.time }}</p>
      </section>

      <!-- The form now only contains necessary, visit-specific fields -->
      <form @submit.prevent="submitBooking">
        
        <!-- 1. Reason for Visit Fieldset -->
        <fieldset>
          <legend><h3>Reason for Visit</h3></legend>
          <label for="reason">Chief Complaint / Symptoms</label>
          <textarea 
            id="reason" 
            v-model="visitReason"
            rows="5" 
            placeholder="Briefly describe why you need to see the doctor. E.g., Rash on left arm, headache for three days, follow-up for medication review."
            required
          ></textarea>
        </fieldset>

        <!-- 2. Necessary Confirmations Fieldset -->
        <fieldset>
          <legend><h3>Pre-Visit Confirmation</h3></legend>

          <div class="checkbox-group">
            <input type="checkbox" id="insurance-check" v-model="insuranceConfirmed" required />
            <label for="insurance-check">I confirm my insurance and billing information on file is current and valid.</label>
          </div>

          <div class="checkbox-group">
            <input type="checkbox" id="policy-check" v-model="policyAcknowledged" required />
            <label for="policy-check">I acknowledge and agree to the clinic's cancellation policy (24-hour notice).</label>
          </div>
        </fieldset>

        <button type="submit">Confirm & Book Appointment</button>
      </form>
    </div>
  </main>
</template>

<style scoped>
.booking-page {
  display: flex;
  justify-content: center;
  align-items: flex-start;
  padding: 100px 20px 50px; /* top padding accounts for fixed header */
  background: white;
  min-height: 100vh;
}

.container {
  max-width: 700px;
  width: 100%;
  background: white;
  border: 1px solid #ccc;
  padding: 30px;
  border-radius: 6px;
}

h2 {
  text-align: center;
  margin-bottom: 30px;
  font-size: 1.5em;
  color: #222;
}

.summary {
  border: 1px solid #ccc;
  padding: 15px;
  margin-bottom: 25px;
  background: #f9f9f9;
}

.summary h3 {
  margin-bottom: 10px;
  font-size: 1.1em;
}

fieldset {
  border: 1px solid #ccc;
  padding: 15px 20px;
  margin-bottom: 25px;
  border-radius: 4px;
}

legend h3 {
  font-size: 1.1em;
  margin: 0;
}

label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
}

/* Updated selector to include textarea */
input[type="text"],
input[type="email"],
input[type="tel"],
input[type="date"],
input[type="password"],
select,
textarea {
  width: 100%;
  padding: 10px;
  margin-bottom: 15px;
  border: 1px solid #aaa;
  border-radius: 4px;
  font-size: 0.95em;
}

.form-row {
  display: flex;
  gap: 20px;
}

.form-row > div {
  flex: 1;
}

.checkbox-group {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 15px;
}

/* Retaining this style block just in case it's used elsewhere, though not in this specific component anymore */
#account-fields {
  border: 1px dashed #aaa;
  padding: 15px;
  border-radius: 4px;
  margin-top: 10px;
  background: #f2f2f2;
}

button {
  display: block;
  width: 100%;
  background: #f5f5f5;
  border: 1px solid #aaa;
  border-radius: 4px;
  padding: 12px;
  cursor: pointer;
  font-size: 1em;
  font-weight: 600;
}

button:hover {
  background: #eaeaea;
}
</style>