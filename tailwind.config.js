const defaultTheme = require('tailwindcss/defaultTheme')

module.exports = {
	content: [
		'./docs/content-leaderboard/**/*.html',
		'./src/Krompaco.RecordCollector.Web/**/*.html',
		'./src/Krompaco.RecordCollector.Web/Components/**/*.razor',
		'./src/Krompaco.RecordCollector.Web/Program.cs',
	],
	theme: {
		extend: {
			fontFamily: {
				sans: ['Funkis Variable', 'Arial', 'Helvetica', 'sans-serif'],
			}
		},
	}
}
