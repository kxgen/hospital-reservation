<template>
    <!-- Footer -->
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
              <li><a href="#">Find a Doctor</a></li>
              <li><a href="#">FAQs</a></li>
              <li><a href="#">Insurance Check</a></li>
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
          &copy; 2025 Hospital App. All rights reserved.
        </div>
      </footer>
</template>

<style scoped>
/* Footer Section */
.footer {
  background: #111;
  color: #ccc;
  padding: 50px 20px 30px;
}

.footer-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
}

.footer h4 {
  color: #fff;
  margin-bottom: 10px;
}

.footer ul {
  list-style: none;
  margin: 0;
  padding: 0;
}

.footer ul li {
  margin-bottom: 5px;
}

.footer ul li a {
  text-decoration: none;
  color: #ccc;
  font-size: 0.9em;
}

.footer ul li a:hover {
  text-decoration: underline;
}

.socials {
  display: flex;
  gap: 10px;
}

.footer-bottom {
  text-align: center;
  margin-top: 30px;
  font-size: 0.8em;
  color: #888;
}

</style>