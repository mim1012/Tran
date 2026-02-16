/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#2E4A7A',
          light: '#3B5998',
          lighter: '#4A6FA5',
        },
        success: '#27AE60',
        warning: '#F39C12',
        danger: '#E74C3C',
        purple: '#8E44AD',
      },
    },
  },
  plugins: [],
}
