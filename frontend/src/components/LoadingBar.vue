<template>
  <div class="loading-container">
    <div class="spinner"></div>
    <p>Fetching data, please wait...</p>
  </div>
</template>

<script setup>
// No script logic needed, it's purely presentation.
</script>

<style scoped>
/* === REUSABLE LOADING ANIMATION STYLES === */

.loading-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 50px;
    border: 1px dashed var(--border-default);
    background: var(--bg-card);
    border-radius: 0;
    color: var(--text-secondary);
    margin-top: 20px;
    width: 100%; /* Ensure it fills its parent container */
}

.spinner {
    border: 4px solid var(--color-gray-100); 
    border-top: 4px solid var(--color-primary); /* Primary color spinner */
    border-radius: 50%;
    width: 40px;
    height: 40px;
    animation: spin 1s linear infinite;
    margin-bottom: 10px;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}
</style>