const defaultTheme = require('tailwindcss/defaultTheme')

module.exports = {
	content: [
		'./docs/content-leaderboard/**/*.html',
		'./src/Krompaco.RecordCollector.Web/Views/**/*.cshtml',
		'./src/Krompaco.RecordCollector.Web/Views/**/*.html',
	],
	theme: {
		extend: {
			fontFamily: {
				sans: ['Inter var', 'Arial', 'Helvetica', 'sans-serif'],
				mono: ['JetBrains Mono', ...defaultTheme.fontFamily.mono],
			}
		},
	}
}
