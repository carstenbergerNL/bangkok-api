/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        surface: {
          DEFAULT: '#f8fafc',
          dark: '#0f172a',
        },
        primary: {
          50: '#f0f9ff',
          100: '#e0f2fe',
          200: '#bae6fd',
          300: '#7dd3fc',
          400: '#38bdf8',
          500: '#0ea5e9',
          600: '#0284c7',
        },
      },
      boxShadow: {
        'soft': '0 1px 2px 0 rgb(0 0 0 / 0.05)',
        'soft-md': '0 2px 4px -1px rgb(0 0 0 / 0.06), 0 4px 6px -2px rgb(0 0 0 / 0.05)',
        'soft-lg': '0 4px 6px -1px rgb(0 0 0 / 0.05), 0 10px 15px -3px rgb(0 0 0 / 0.05)',
        'card': '0 1px 2px 0 rgb(0 0 0 / 0.05), 0 2px 6px -2px rgb(0 0 0 / 0.05)',
      },
      borderRadius: {
        'xl': '0.5rem',
        '2xl': '0.75rem',
      },
      transitionDuration: {
        '200': '200ms',
        '300': '300ms',
      },
    },
  },
  plugins: [],
}
