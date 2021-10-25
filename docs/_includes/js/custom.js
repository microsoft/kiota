jtd.onReady(function() {
  const theme = localStorage.getItem('theme');
  setTheme(theme);
});

function switchTheme() {
  const theme = localStorage.getItem('theme');
  const newTheme = theme === 'dark' ? 'light' : 'dark';
  setTheme(newTheme);
}

function setTheme(theme) {
  theme = theme === 'dark' ? 'dark' : 'light';
  jtd.setTheme(theme);
  localStorage.setItem('theme', theme);

  const buttonText = theme === 'dark' ? 'Light mode' : 'Dark mode';
  document.getElementById('theme-switch').innerHTML = buttonText;
}
