<template>
  <div class="doctor-container">
    <div class="page-title">
      <h1>Staff <span>Registration</span></h1>
      <p>Configure and onboard new medical or administrative personnel.</p>
    </div>

    <div class="form-box shadow-lg">
      <div class="form-header">
        <h2>Account <span>Details</span></h2>
        <p>Enter the required information to create a system account.</p>
      </div>

      <form @submit.prevent="submit" class="staff-form">
        <div class="form-row">
          <div class="form-group">
            <label>Staff Role</label>
            <select v-model="form.role" required class="form-input">
              <option :value="2">Doctor</option>
              <option :value="3">Receptionist</option>
            </select>
          </div>

          <div class="form-group" v-if="form.role === 2">
            <label>Specialty</label>
            <select v-model="form.specialtyId" required class="form-input">
              <option disabled value="">Select Specialty</option>
              <option v-for="s in specialties" :key="s.id" :value="s.id">
                {{ s.name }}
              </option>
            </select>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>First Name</label>
            <input v-model="form.firstName" required class="form-input" placeholder="e.g. Abebe" />
          </div>
          <div class="form-group">
            <label>Last Name</label>
            <input v-model="form.lastName" required class="form-input" placeholder="e.g. Kebede" />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>Email Address</label>
            <input type="email" v-model="form.email" required class="form-input" placeholder="name@clinic.com" />
          </div>
          <div class="form-group">
            <label>Phone Number</label>
            <input v-model="form.phone" class="form-input" placeholder="+251..." />
          </div>
        </div>

        <div class="form-group">
          <label>Gender</label>
          <div class="radio-group">
            <label class="radio-label">
              <input type="radio" value="female" v-model="form.gender" required />
              <span>Female</span>
            </label>
            <label class="radio-label">
              <input type="radio" value="male" v-model="form.gender" required />
              <span>Male</span>
            </label>
          </div>
        </div>

        <div class="form-group">
          <label>Temporary Password</label>
          <input type="text" v-model="form.password" class="form-input" placeholder="e.g. 123456" />
          <p class="input-hint">User will be forced to change this upon their first login.</p>
        </div>

        <div class="form-actions">
          <button class="btn-submit">Create Account</button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import axios from "@/api/axios";
import { useAlertStore } from '@/stores/alertStore'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore();
const token = auth.token; 

const specialties = ref([]);
const alertStore = useAlertStore();

const form = ref({
  firstName: "",
  lastName: "",
  gender: "",
  email: "",
  phone: "",
  role: 2,
  specialtyId: "",
  password: "123456"
});

const loadSpecialties = async () => {
    try {
        const res = await axios.get("/api/doctors/specialties");
        specialties.value = res.data; // [{id, name}, ...]
    } catch (err) {
        console.error("Failed to load specialties", err);
    }
}

onMounted(loadSpecialties);

const submit = async () => {
  const payload = {
    FirstName: form.value.firstName,
    LastName: form.value.lastName,
    Gender: form.value.gender,
    Email: form.value.email,
    Phone: form.value.phone,
    Role: form.value.role,
    SpecialtyId: form.value.specialtyId,
    Password: form.value.password,
  };

  if (payload.Role !== 2) payload.SpecialtyId = 0;

  try {
    await axios.post(
      "/api/admin/createStaff",
      payload,
      {
        headers: {
          Authorization: `Bearer ${token}`
        }
      }
    );

    alertStore.showAlert("Staff registered successfully.", "success");

    form.value = {
      firstName: "", lastName: "", gender: "", email: "", phone: "",
      role: 2, specialtyId: "", password: "123456"
    };
  } catch (error) {
    const msg = error.response?.data?.Message || "An unknown error occurred.";
    alertStore.showAlert("Registration failed: " + msg, "error");
  }
};
</script>

<style scoped>
/* Inherited from your Layout for consistency */
.form-header {
    margin-bottom: 25px;
}

.form-header h2 {
    font-weight: 800;
    margin: 0;
}

.form-header h2 span {
    color: var(--color-primary);
}

.form-header p {
    color: var(--text-secondary);
    margin-top: 5px;
    font-size: 0.9rem;
}

/* Grid for Two-Column Rows */
.form-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 25px;
}

@media (max-width: 600px) {
    .form-row {
        grid-template-columns: 1fr;
        gap: 15px; /* Added gap for stacked mobile inputs */
    }
}

/* Custom Radio Styling */
.radio-group {
    display: flex;
    gap: 25px;
    padding: 10px 0;
}

.radio-label {
    display: flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    font-weight: 600;
    color: var(--text-primary);
}

.radio-label input[type="radio"] {
    accent-color: var(--color-primary);
    width: 18px;
    height: 18px;
}

.input-hint {
    font-size: 0.75rem;
    color: var(--text-tertiary);
    margin-top: 5px;
    font-style: italic;
}

.form-actions {
    margin-top: 30px;
}

/* Ensure the layout classes from your Layout.vue are applied if not global */
.doctor-container {
    max-width: 800px;
    margin: 40px auto;
}

.form-box {
    background: var(--bg-card);
    padding: 40px;
    border-radius: 15px;
}

.form-group {
    margin-bottom: 20px;
    display: flex;
    flex-direction: column;
}

.form-group label {
    font-weight: 800;
    font-size: var(--font-size-small);
    color: var(--text-secondary);
    text-transform: uppercase;
    margin-bottom: 8px;
}

.form-input {
    padding: 12px 15px;
    border: 1px solid var(--border-input);
    border-radius: 8px;
    font-family: inherit;
    font-size: var(--font-size-base);
    transition: border-color 0.2s;
}

.form-input:focus {
    border-color: var(--color-primary);
    outline: none;
    box-shadow: 0 0 0 3px var(--color-primary-glow);
}

.btn-submit {
    width: 100%;
    background: var(--btn-primary-bg);
    color: var(--color-white);
    border: none;
    padding: 16px;
    border-radius: 8px;
    font-weight: 800;
    font-size: 1rem;
    cursor: pointer;
    transition: background 0.3s;
}

.btn-submit:hover {
    background: var(--color-primary);
}

.breadcrumb a {
    color: var(--color-primary);
    text-decoration: none;
    font-weight: 700;
    margin-bottom: 20px;
    display: inline-block;
}
</style>