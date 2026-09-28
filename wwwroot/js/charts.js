(function () {
    const palette = {
        ink: "#1a1823",
        saffron: "#e9a21b",
        clay: "#c4553b",
        slate: "#3e5c76",
        line: "#e4dccf",
        muted: "#7a7482"
    };

    function applyTheme() {
        Chart.defaults.font.family = '"Inter", system-ui, sans-serif';
        Chart.defaults.font.size = 12;
        Chart.defaults.color = palette.muted;
        Chart.defaults.borderColor = palette.line;
        Chart.defaults.plugins.legend.position = "bottom";
        Chart.defaults.plugins.legend.labels.usePointStyle = true;
        Chart.defaults.plugins.legend.labels.boxWidth = 8;
        Chart.defaults.plugins.legend.labels.padding = 18;
        Chart.defaults.plugins.tooltip.backgroundColor = palette.ink;
        Chart.defaults.plugins.tooltip.padding = 10;
        Chart.defaults.plugins.tooltip.cornerRadius = 8;
        Chart.defaults.plugins.tooltip.titleFont = { family: '"Outfit", "Inter", sans-serif', size: 13 };
    }

    function dayLabel(value) {
        const date = new Date(value);
        return date.toLocaleDateString(undefined, { day: "numeric", month: "short" });
    }

    function dayKey(value) {
        return String(value).substring(0, 10);
    }

    function render(canvas, config) {
        if (canvas.chart) {
            canvas.chart.destroy();
        }
        canvas.chart = new Chart(canvas, config);
    }

    function setEmpty(id, isEmpty) {
        const element = document.getElementById(id);
        if (element) {
            element.classList.toggle("d-none", !isEmpty);
        }
    }

    function baseOptions() {
        return {
            maintainAspectRatio: false,
            interaction: { mode: "index", intersect: false },
            scales: {
                x: { grid: { display: false } },
                y: { grid: { color: palette.line }, border: { display: false } }
            }
        };
    }

    function progress(url) {
        applyTheme();
        const weightCanvas = document.getElementById("weightChart");
        const calorieCanvas = document.getElementById("calorieChart");
        const buttons = document.querySelectorAll("[data-range]");

        function load(days) {
            fetch(url + "?days=" + days)
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    setEmpty("weightEmpty", data.weight.length === 0);
                    const weightOptions = baseOptions();
                    weightOptions.scales.y1 = { position: "right", grid: { drawOnChartArea: false }, border: { display: false } };
                    render(weightCanvas, {
                        type: "line",
                        data: {
                            labels: data.weight.map(function (p) { return dayLabel(p.date); }),
                            datasets: [
                                {
                                    label: "Weight (kg)",
                                    data: data.weight.map(function (p) { return p.weightKg; }),
                                    borderColor: palette.ink,
                                    backgroundColor: palette.ink,
                                    borderWidth: 2.5,
                                    tension: 0.35,
                                    pointRadius: 4,
                                    pointBackgroundColor: palette.saffron,
                                    pointBorderColor: palette.ink,
                                    pointBorderWidth: 2,
                                    yAxisID: "y"
                                },
                                {
                                    label: "BMI",
                                    data: data.weight.map(function (p) { return p.bmi; }),
                                    borderColor: palette.saffron,
                                    backgroundColor: palette.saffron,
                                    borderDash: [6, 5],
                                    borderWidth: 2,
                                    tension: 0.35,
                                    pointRadius: 0,
                                    yAxisID: "y1"
                                }
                            ]
                        },
                        options: weightOptions
                    });

                    const totals = {};
                    data.caloriesBurned.forEach(function (d) {
                        totals[dayKey(d.date)] = totals[dayKey(d.date)] || { burned: 0, consumed: 0 };
                        totals[dayKey(d.date)].burned = d.total;
                    });
                    data.caloriesConsumed.forEach(function (d) {
                        totals[dayKey(d.date)] = totals[dayKey(d.date)] || { burned: 0, consumed: 0 };
                        totals[dayKey(d.date)].consumed = d.total;
                    });
                    const keys = Object.keys(totals).sort();
                    setEmpty("calorieEmpty", keys.length === 0);
                    render(calorieCanvas, {
                        type: "bar",
                        data: {
                            labels: keys.map(dayLabel),
                            datasets: [
                                { label: "Consumed (kcal)", data: keys.map(function (k) { return totals[k].consumed; }), backgroundColor: palette.slate, borderRadius: 6, maxBarThickness: 22 },
                                { label: "Burned (kcal)", data: keys.map(function (k) { return totals[k].burned; }), backgroundColor: palette.clay, borderRadius: 6, maxBarThickness: 22 }
                            ]
                        },
                        options: baseOptions()
                    });
                });
        }

        buttons.forEach(function (button) {
            button.addEventListener("click", function () {
                buttons.forEach(function (b) { b.classList.remove("active"); });
                button.classList.add("active");
                load(button.getAttribute("data-range"));
            });
        });

        load(30);
    }

    function usage(url) {
        applyTheme();
        const canvas = document.getElementById("usageChart");
        fetch(url + "?days=14")
            .then(function (response) { return response.json(); })
            .then(function (data) {
                const options = baseOptions();
                options.scales.x.stacked = true;
                options.scales.y.stacked = true;
                options.scales.y.ticks = { precision: 0 };
                render(canvas, {
                    type: "bar",
                    data: {
                        labels: data.labels.map(dayLabel),
                        datasets: [
                            { label: "Workouts", data: data.workouts, backgroundColor: palette.ink, borderRadius: 4, maxBarThickness: 26 },
                            { label: "Meals", data: data.meals, backgroundColor: palette.saffron, borderRadius: 4, maxBarThickness: 26 },
                            { label: "Measurements", data: data.metrics, backgroundColor: palette.slate, borderRadius: 4, maxBarThickness: 26 },
                            { label: "New members", data: data.registrations, type: "line", borderColor: palette.clay, backgroundColor: palette.clay, tension: 0.35, pointRadius: 3, stack: "registrations" }
                        ]
                    },
                    options: options
                });
            });
    }

    window.HealthTrackCharts = { progress: progress, usage: usage };
})();
