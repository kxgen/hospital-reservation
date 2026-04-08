import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import '@/assets/portal.css'
import dateFormatter from '@/utils/dateFormatter'

const app = createApp(App)
app.use(createPinia())
app.use(router)

// Global properties for easy access in templates
app.config.globalProperties.$dt = dateFormatter

app.mount('#app')
