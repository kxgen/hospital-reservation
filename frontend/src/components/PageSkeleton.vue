<template>
  <div class="page-skeleton">
    <div class="skeleton-header">
      <Skeleton width="40%" height="40px" borderRadius="8px" />
      <Skeleton width="60%" height="20px" borderRadius="4px" style="margin-top: 10px" />
    </div>

    <div class="skeleton-grid">
      <div v-for="i in 3" :key="i" class="skeleton-card">
        <div class="card-header">
           <Skeleton width="30%" height="24px" />
        </div>
        <div class="card-body">
           <Skeleton width="100%" height="16px" style="margin-bottom: 10px" />
           <Skeleton width="100%" height="16px" style="margin-bottom: 10px" />
           <Skeleton width="70%" height="16px" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import Skeleton from './Skeleton.vue'
</script>

<style scoped>
.page-skeleton {
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

.skeleton-header {
  margin-bottom: 40px;
}

.skeleton-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 25px;
}

.skeleton-card {
  background: var(--bg-surface, white);
  border: 1px solid var(--border-default, #eee);
  border-radius: 16px;
  padding: 20px;
}

.card-header {
  margin-bottom: 20px;
}

.card-body {
  display: flex;
  flex-direction: column;
}
</style>
