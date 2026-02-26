/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx,css}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      fontFamily: {
        sans: ['"Segoe UI"', '"Segoe UI Web (West European)"', 'Segoe UI', '-apple-system', 'BlinkMacSystemFont', 'Roboto', 'Helvetica Neue', 'sans-serif'],
      },
      colors: {
        /* Fluent / Microsoft portal palette */
        fluent: {
          background: '#f3f2f1',
          surface: '#ffffff',
          surfaceAlt: '#faf9f8',
          border: '#edebe9',
          borderStrong: '#d2d0ce',
          text: '#323130',
          textSecondary: '#605e5c',
          primary: '#0078d4',
          primaryHover: '#106ebe',
          primaryPressed: '#005a9e',
        },
        primary: {
          50: '#e6f4ff',
          100: '#cce9ff',
          200: '#99d3ff',
          300: '#66b8f7',
          400: '#3399e8',
          500: '#0078d4',
          600: '#106ebe',
          700: '#005a9e',
        },
      },
      spacing: {
        '4.5': '1.125rem',
        '18': '4.5rem',
        '22': '5.5rem',
      },
      borderRadius: {
        'sm': '2px',
        'DEFAULT': '4px',
        'card': '4px',
        'input': '2px',
        'btn': '4px',
      },
      boxShadow: {
        'card': '0 1.6px 3.6px 0 rgba(0, 0, 0, 0.132), 0 0.3px 0.9px 0 rgba(0, 0, 0, 0.108)',
        'dropdown': '0 3.2px 7.2px 0 rgba(0, 0, 0, 0.132), 0 0.6px 1.8px 0 rgba(0, 0, 0, 0.108)',
        'modal': '0 6.4px 14.4px 0 rgba(0, 0, 0, 0.132), 0 1.2px 3.6px 0 rgba(0, 0, 0, 0.108)',
      },
      transitionDuration: {
        '150': '150ms',
        '200': '200ms',
      },
    },
  },
  plugins: [],
}
