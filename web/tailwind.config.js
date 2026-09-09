/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#eef7f4', 100: '#d6ece4', 500: '#0f766e', 600: '#0d6a63', 700: '#0b544f',
        },
      },
    },
  },
  plugins: [],
};
