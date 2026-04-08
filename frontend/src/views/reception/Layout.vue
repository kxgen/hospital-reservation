<script setup>
import Header from '@/layouts/Header.vue'
import Footer from '@/layouts/Footer.vue'
import { useAuthStore } from '@/stores/auth'
import PageSkeleton from '@/components/PageSkeleton.vue'

const auth = useAuthStore()
</script>

<template>
  <div class="reception-layout">
    <Header />

    <main class="page-area">
      <div class="content-wrapper">
        <template v-if="!auth.isLoaded">
          <PageSkeleton />
        </template>
        <template v-else>
          <router-view />
        </template>
      </div>
      <Footer />
    </main>
  </div>
</template>

<style scoped>
.reception-layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background-color: var(--bg-body);
  padding-top: 64px;
}

.page-area {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.content-wrapper {
  flex: 1;
  padding: 3rem 4rem; /* More "breath" on the sides */
  max-width: 1500px; /* Slightly wider to accommodate padding */
  width: 100%;
  margin: 0 auto;
  box-sizing: border-box;
}

/* Tablet View */
@media (max-width: 1024px) {
  .content-wrapper { padding: 30px 20px; }
}

/* Mobile View */
@media (max-width: 768px) {
  .content-wrapper { padding: 20px 15px; }
}
</style>