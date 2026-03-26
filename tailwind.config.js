/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Components/**/*.{razor,html}",
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
