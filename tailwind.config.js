const defaultTheme = require('tailwindcss/defaultTheme')

module.exports = {
	content: [
		'./docs/content-leaderboard/**/*.html',
		'./src/Krompaco.RecordCollector.Web/Views/**/*.cshtml',
		'./src/Krompaco.RecordCollector.Web/Views/**/*.html',
		'./src/Krompaco.RecordCollector.Web/Program.cs',
	],
	theme: {
		extend: {
			fontFamily: {
				sans: ['Inter var', 'Arial', 'Helvetica', 'sans-serif'],
			}
		},
	}
}
