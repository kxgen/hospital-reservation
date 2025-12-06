<template>
    <div class="layout">
      <aside class="sidebar">
        <h2 class="logo">Patient</h2>
        <nav>
          <RouterLink to="/dashboard-patient">Dashboard</RouterLink>
          <RouterLink to="/patient/appointments">My Appointments</RouterLink>
          <RouterLink to="/patient/book">Book Appointment</RouterLink>
          <RouterLink to="/patient/history">Medical History</RouterLink>
          <RouterLink to="/patient/profile">Profile</RouterLink>
        </nav>
      </aside>
  
      <main class="content">
        <slot />
      </main>
    </div>
  </template>
  
  <style scoped>
  .layout { display: flex; height: 100vh; }
  .sidebar {
    width: 250px;
    background: #0d6efd;
    padding: 20px;
    color: #fff;
    display: flex;
    flex-direction: column;
  }
  .sidebar a {
    color: white;
    text-decoration: none;
    margin: 10px 0;
    display: block;
  }
  .content {
    flex: 1;
    padding: 20px;
    background: #f5f6fa;
  }
  </style>
  