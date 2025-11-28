<script setup>
  import { ref, onMounted } from 'vue';
  import { useRouter } from 'vue-router';

  const router = useRouter();
  const isLoggedIn = ref(false);

  // Check login state on mount
  onMounted(() => {
    const token = localStorage.getItem('token');
    isLoggedIn.value = !!token;
  });

  function logout() {
    localStorage.removeItem('token');
    isLoggedIn.value = false;
    router.push('/login');
  }
</script>

<template>
  <header class="header">
    <div class="header-inner">
      <router-link to="/" class="logo">
        Hospital<span>Booking</span>App
      </router-link>

      <nav class="nav">
        <!-- if NOT logged in -->
        <router-link v-if="!isLoggedIn" to="/login">Login</router-link>
        <router-link v-if="!isLoggedIn" to="/register">Register</router-link>

        <!-- if logged in -->
        <router-link v-if="isLoggedIn" to="/patient">Patient</router-link>
        <button v-if="isLoggedIn" @click="logout">Logout</button>
      </nav>
      <button class="menu-btn">☰</button>
    </div>
  </header>
</template>

<style scoped>
/* Header container */
.header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  background: #fff;
  border-bottom: 1px solid #ccc;
  height: 60px;
  display: flex;
  align-items: center;
  z-index: 10;
}

/* Inner content alignment */
.header-inner {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 90%;
  max-width: 1200px;
  margin: 0 auto;
}

/* Logo */
.logo {
  font-weight: bold;
  font-size: 1.4em;
  color: #000;
  text-decoration: none;
}

/* Slight visual distinction (still minimal) */
.logo span {
  font-weight: normal;
}

/* Navigation links */
.nav {
  display: flex;
  gap: 24px;
  align-items: center;
}

.nav a,
.nav router-link {
  color: #000;
  text-decoration: none;
  font-size: 0.95em;
}

.nav a:hover,
.nav router-link:hover {
  text-decoration: underline;
}

/* Menu button for small screens */
.menu-btn {
  display: none;
  background: none;
  border: none;
  font-size: 1.5em;
  cursor: pointer;
}

/* Responsive layout */
@media (max-width: 768px) {
  .nav {
    display: none;
  }

  .menu-btn {
    display: block;
  }
}
</style>
