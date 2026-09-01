/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    "./*.razor",
    "./Components/**/*.{razor,html}",
    "../Boodschap.Mobile/**/*.{razor,html}",
    "../Features/**/*.{razor,html}",
    "../Shared/**/*.{razor,html}",
    "./wwwroot/**/*.html",
    "./wwwroot/js/**/*.js"
  ],
  theme: {
    extend: {}
  },
  plugins: []
};
