/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./*.razor",
    "./Components/**/*.{razor,html}",
    "./Features/**/*.{razor,html}",
    "./Shared/**/*.{razor,html}",
    "./Pages/**/*.{razor,html}",
    "./Layout/**/*.{razor,html}",
    "./wwwroot/**/*.html",
    "./wwwroot/js/**/*.js"
  ],
  theme: {
    extend: {}
  },
  plugins: []
};
