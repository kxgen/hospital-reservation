<script setup>
  const currentYear = new Date().getFullYear();
</script>

<template>
    
    <footer class="footer">
        <div class="container footer-grid">
          <div>
            <h4>Company</h4>
            <ul>
              <li><a href="#">About Us</a></li>
              <li><a href="#">Careers</a></li>
              <li><a href="#">Press</a></li>
              <li><a href="#">Blog</a></li>
            </ul>
          </div>
          <div>
            <h4>For Patients</h4>
            <ul>
              <li><a href="#">FAQs</a></li>
              <li><a href="#">Contact Support</a></li>
            </ul>
          </div>
          <div>
            <h4>Connect</h4>
            <p>Follow us on social media.</p>
            <div class="socials">
              <a href="#">Facebook</a>
              <a href="#">Instagram</a>
            </div>
          </div>
        </div>
        <div class="footer-bottom">
          <p>
          &copy; {{ currentYear }} 
          <span class="brand">Trinity Specialized Center</span>. 
          All rights reserved.
        </p>
        </div>
      </footer>
</template>

<style scoped>



.container {
    width: 90%;
    max-width: 1200px;
    margin: 0 auto;
}


.footer {
  background: var(--color-gray-900); 
  color: var(--color-gray-400); 
  padding: 60px 20px 20px;
  font-family: inherit;
}

.footer-grid {
  display: grid;
  
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); 
  gap: 30px;
  padding-bottom: 40px;
}

.footer h4 {
  color: var(--color-white); 
  margin-bottom: 15px;
  font-size: 1.15em;
  font-weight: 700;
  border-left: 3px solid var(--color-primary); 
  padding-left: 10px;
}

.footer ul {
  list-style: none;
  margin: 0;
  padding: 0;
}

.footer ul li {
  margin-bottom: 10px; 
}

.footer ul li a {
  text-decoration: none;
  color: var(--color-gray-400);
  font-size: 1em;
  transition: color 0.2s;
}

.footer ul li a:hover {
  color: var(--color-primary); 
  text-decoration: none;
}

.footer p {
    font-size: 1em;
    color: var(--color-gray-400);
    line-height: 1.5;
}


.socials {
  display: flex;
  gap: 15px;
  margin-top: 15px;
}

.socials a {
    text-decoration: none;
    color: var(--color-white);
    font-weight: 600;
    padding: 8px 12px;
    border: 1px solid var(--color-gray-700); 
    border-radius: 4px;
    background: var(--color-gray-800); 
    transition: all 0.3s;
}

.socials a:hover {
    background: var(--color-primary); 
    color: var(--color-white);
    border-color: var(--color-primary);
}


.footer-bottom {
  text-align: center;
  margin-top: 30px;
  padding-top: 20px;
  border-top: 1px solid var(--color-gray-700); 
  font-size: 0.9em;
  color: var(--color-gray-500);
}


@media (max-width: 768px) {
    .footer-grid {
        grid-template-columns: 1fr;
    }
    .footer h4 {
        margin-top: 20px;
    }
}
</style>