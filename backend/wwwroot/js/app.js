// Check which page we're on
document.addEventListener("DOMContentLoaded", () => {
  if (document.getElementById("loginForm")) {
    setupLoginPage();
  } else if (document.querySelector("#registerForm")) {
    setupRegistrationPage();
  } else if (document.getElementById("userDataList")) {
    setupDashboardPage();
  }
});

// ---------------- LOGIN PAGE ----------------
function setupLoginPage() {
  const form = document.getElementById("loginForm");
  const message = document.getElementById("message");

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      if (!response.ok) {
        message.textContent = "Login failed: Invalid username or password";
        return;
      }

      const data = await response.json();
      localStorage.setItem("jwt", data.token);

      // Redirect to dashboard
      window.location.href = "dashboard.html";
    } catch (err) {
      console.error(err);
      message.textContent = "Error connecting to server";
    }
  });
}

// ---------------- Registration PAGE ----------------
function setupRegistrationPage() {
  const form = document.querySelector("#registerForm");
  const message = document.querySelector("#message");
  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

    try {
      const response = await fetch("api/auth/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      if (!response.ok) {
        message.textContent =
          "Registration failed: Invalid username or password";
        return;
      }

      // Redirect to dashboard
      window.location.href = "login.html";
    } catch (err) {
      console.error(err);
      message.textContent = "Error connecting to server";
    }
  });
}

// ---------------- DASHBOARD PAGE ----------------
function setupDashboardPage() {
  const list = document.getElementById("userDataList");
  const logoutBtn = document.getElementById("logoutButton");

  const token = localStorage.getItem("jwt");
  if (!token) {
    alert("You must log in first");
    window.location.href = "login.html";
    return;
  }

  fetch("/api/users", {
    // replace with your protected API endpoint
    headers: { Authorization: `Bearer ${token}` },
  })
    .then((res) => {
      if (!res.ok) throw new Error("Unauthorized");
      return res.json();
    })
    .then((users) => {
      list.innerHTML = "";
      users.forEach((user) => {
        const li = document.createElement("li");
        li.textContent = `${user.username} (${user.role})`;
        list.appendChild(li);
      });
    })
    .catch((err) => {
      console.error(err);
      list.innerHTML = "<li>Failed to load user data</li>";
    });

  logoutBtn.addEventListener("click", () => {
    localStorage.removeItem("jwt");
    window.location.href = "login.html";
  });
}
