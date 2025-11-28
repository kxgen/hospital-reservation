<script setup>
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <div class="patient-portal">
    <div class="portal-layout">

      <!-- Sidebar -->
      <aside class="sidebar">

        <!-- Top section: Welcome + Logout -->
        <div class="sidebar-header">
          <h2 class="sidebar-title">Welcome Abebe</h2>
          <RouterLink to="/" class="logout">Logout</RouterLink>
        </div>

        <!-- Navigation -->
        <nav class="sidebar-nav">
          <RouterLink to="/" class="nav-item"><-</RouterLink>
          <RouterLink to="/portal/dashboard" class="nav-item" active-class="active">Dashboard</RouterLink>
          <RouterLink to="/portal/appointments" class="nav-item" active-class="active">Appointments</RouterLink>
          <RouterLink to="/portal/history" class="nav-item" active-class="active">History</RouterLink>
          <RouterLink to="/portal/profile" class="nav-item" active-class="active">Settings</RouterLink>
        </nav>

      </aside>

      <!-- Main Content -->
      <main class="portal-content">
        <RouterView />
      </main>

    </div>
  </div>
</template>

<style scoped>
.patient-portal {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: #f9f9f9;
}

.portal-layout {
  display: flex;
  flex: 1;
}

/* Sidebar */
.sidebar {
  width: 250px;
  background: #fff;
  border-right: 1px solid #ddd;
  padding: 20px;
  display: flex;
  flex-direction: column;
}

.sidebar-title {
  font-size: 1.4em;
  font-weight: bold;
  color: #000;
  margin-bottom: 10px;
}

.sidebar-header .logout {
  margin-bottom: 20px;
  display: inline-block;
}

/* Nav */
.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.nav-item {
  text-decoration: none;
  color: #333;
  padding: 10px;
  border-radius: 5px;
  transition: background 0.3s;
}

.nav-item:hover {
  background: #f3f3f3;
}

.active {
  background: #000;
  color: #fff !important;
}

.logout {
  color: #d00;
  text-decoration: none;
  font-weight: 500;
}

.logout:hover {
  text-decoration: underline;
}

.portal-content {
  flex: 1;
  padding: 30px;
  background: #fafafa;
}

/* Responsive */
@media (max-width: 768px) {
  .portal-layout {
    flex-direction: column;
  }

  .sidebar {
    width: 100%;
    border-right: none;
    border-bottom: 1px solid #ddd;
    flex-direction: row;
    align-items: center;
    justify-content: space-around;
  }
}
</style>
