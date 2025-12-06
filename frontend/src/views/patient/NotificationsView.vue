<template>
  <div class="page">
    <h1 class="title">Notifications</h1>

    <div class="notifications">
      <div class="note">
        <h3>Appointment Reminder</h3>
        <p>You have an appointment with Dr. Fikre tomorrow at 9:00 AM.</p>
      </div>

      <div class="note">
        <h3>New Lab Result</h3>
        <p>Your blood test result is now available in your history page.</p>
      </div>

      <div class="note">
        <h3>Profile Update</h3>
        <p>Your profile was updated successfully.</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page {
  padding: 10px;
  background: #fafafa;
}
.title {
  font-size: 1.8em;
  margin-bottom: 20px;
  font-weight: bold;
  color: #000;
}

.notifications {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.note {
  background: #fff;
  padding: 20px;
  border: 1px solid #ddd;
  border-left: 5px solid #000;
  border-radius: 8px;
}

.note h3 {
  font-size: 1.2em;
  margin-bottom: 8px;
  color: #000;
}
</style>
