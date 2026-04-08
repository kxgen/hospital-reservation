<template>
  <div 
    class="skeleton-box" 
    :style="{ 
      width: width, 
      height: height, 
      borderRadius: borderRadius 
    }"
  ></div>
</template>

<script setup>
defineProps({
  width: { type: String, default: '100%' },
  height: { type: String, default: '20px' },
  borderRadius: { type: String, default: '4px' }
})
</script>

<style scoped>
.skeleton-box {
  background: var(--color-gray-200, #e5e7eb);
  position: relative;
  overflow: hidden;
  display: inline-block;
}

.skeleton-box::after {
  content: "";
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  transform: translateX(-100%);
  background-image: linear-gradient(
    90deg,
    rgba(255, 255, 255, 0) 0,
    rgba(255, 255, 255, 0.2) 20%,
    rgba(255, 255, 255, 0.5) 60%,
    rgba(255, 255, 255, 0)
  );
  animation: shimmer 1.5s infinite;
}

@keyframes shimmer {
  100% {
    transform: translateX(100%);
  }
}
</style>
