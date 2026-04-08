<script setup>
import Header from '@/layouts/Header.vue'
import Footer from '@/layouts/Footer.vue';
import { useAuthStore } from '@/stores/auth';
import PageSkeleton from '@/components/PageSkeleton.vue';

const auth = useAuthStore();
</script>

<template>
  <div class="layout">
    <Header />
    <div class="page-with-header">
      <template v-if="!auth.isLoaded">
        <PageSkeleton />
      </template>
      <template v-else>
        <router-view />
      </template>
    </div>
    <Footer />
  </div>
</template>

<style scoped>
.layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  padding-top: 60px;
}
.page-with-header {
  flex: 1;
}
</style>
