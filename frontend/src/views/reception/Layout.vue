<script setup>
import Header from '@/components/Header.vue'
</script>

<template>
  <div class="layout">
    <Header />
    <div class="page-with-header">
      <router-view />
    </div>
  </div>
</template>

<style scoped>
  .layout {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
  }
  .page-with-header {
    border: thin solid red;
    flex: 1;
  }
</style>