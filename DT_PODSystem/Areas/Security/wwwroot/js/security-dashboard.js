// Areas/Security/wwwroot/js/security-dashboard.js
var Dashboard = (function () {
    'use strict';

    var charts = {};
    var refreshInterval;
    var settings = {
        refreshInterval: 300000, // 5 minutes
        chartColors: ['#667eea', '#f093fb', '#4facfe', '#43e97b', '#fa709a', '#ffeaa7', '#fd79a8', '#fdcb6e'],
        animationDuration: 1000
    };

    function init() {
        initializeCharts();
        loadAllData();
        setupEventHandlers();
        startAutoRefresh();

        console.log('Security Dashboard initialized successfully');
    }

    function initializeCharts() {
        // Initialize ApexCharts for permission distribution
        var permissionChartOptions = {
            series: [],
            chart: {
                type: 'donut',
                height: 300,
                fontFamily: 'inherit',
                animations: {
                    enabled: true,
                    easing: 'easeinout',
                    speed: settings.animationDuration
                },
                toolbar: {
                    show: false
                }
            },
            colors: settings.chartColors,
            labels: [],
            legend: {
                position: 'bottom',
                horizontalAlign: 'center',
                fontSize: '14px',
                markers: {
                    width: 10,
                    height: 10,
                    strokeWidth: 0,
                    strokeColor: '#fff',
                    fillColors: undefined,
                    radius: 12
                }
            },
            plotOptions: {
                pie: {
                    donut: {
                        size: '60%',
                        labels: {
                            show: true,
                            total: {
                                show: true,
                                showAlways: true,
                                fontSize: '16px',
                                fontWeight: 600,
                                color: '#2c3e50'
                            }
                        }
                    }
                }
            },
            dataLabels: {
                enabled: true,
                formatter: function (val, opts) {
                    return opts.w.config.series[opts.seriesIndex];
                },
                style: {
                    fontSize: '12px',
                    fontWeight: 600
                }
            },
            tooltip: {
                theme: 'dark',
                fillSeriesColor: false,
                y: {
                    formatter: function (val, opts) {
                        return val + ' permissions (' + opts.series[opts.seriesIndex] + '%)';
                    }
                }
            },
            responsive: [{
                breakpoint: 768,
                options: {
                    chart: {
                        height: 250
                    },
                    legend: {
                        position: 'bottom'
                    }
                }
            }]
        };

        charts.permissionDistribution = new ApexCharts(
            document.querySelector("#permissionDistributionChart"),
            permissionChartOptions
        );
        charts.permissionDistribution.render();
    }

    function loadAllData() {
        loadStatistics();
        loadPermissionDistribution();
        loadRoleDistribution();
        loadRecentActivity();
        loadSystemHealth();
    }

    function loadStatistics() {
        $.ajax({
            url: '/Security/Dashboard/GetStatisticsData',
            type: 'GET',
            beforeSend: function () {
                showStatisticsLoading(true);
            },
            success: function (response) {
                if (response.success) {
                    updateStatistics(response.data);
                } else {
                    console.error('Error loading statistics:', response.error);
                    alert.error('Failed to load statistics: ' + response.error);
                }
            },
            error: function (xhr, status, error) {
                console.error('AJAX error loading statistics:', error);
                alert.error('Failed to load statistics. Please try again.');
            },
            complete: function () {
                showStatisticsLoading(false);
            }
        });
    }

    function loadPermissionDistribution() {
        $.ajax({
            url: '/Security/Dashboard/GetPermissionDistribution',
            type: 'GET',
            beforeSend: function () {
                showChartLoading('permissionChartRefresh', true);
            },
            success: function (response) {
                if (response.success && response.data.length > 0) {
                    updatePermissionDistributionChart(response.data);
                } else {
                    console.warn('No permission distribution data available');
                    showEmptyPermissionChart();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading permission distribution:', error);
                showEmptyPermissionChart();
                alert.error('Failed to load permission distribution chart.');
            },
            complete: function () {
                showChartLoading('permissionChartRefresh', false);
            }
        });
    }

    function loadRoleDistribution() {
        $.ajax({
            url: '/Security/Dashboard/GetRoleDistribution',
            type: 'GET',
            beforeSend: function () {
                showChartLoading('rolesRefresh', true);
                $('#roleDistributionContainer').html('<div class="text-center py-4"><div class="loading-spinner"></div><div class="mt-2">Loading roles...</div></div>');
            },
            success: function (response) {
                if (response.success) {
                    updateRoleDistribution(response.data);
                } else {
                    console.error('Error loading role distribution:', response.error);
                    showEmptyRoleDistribution();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading role distribution:', error);
                showEmptyRoleDistribution();
            },
            complete: function () {
                showChartLoading('rolesRefresh', false);
            }
        });
    }

    function loadRecentActivity() {
        $.ajax({
            url: '/Security/Dashboard/GetRecentActivity',
            type: 'GET',
            beforeSend: function () {
                showChartLoading('activityRefresh', true);
                $('#recentActivityContainer').html('<div class="text-center py-4"><div class="loading-spinner"></div><div class="mt-2">Loading activities...</div></div>');
            },
            success: function (response) {
                if (response.success) {
                    updateRecentActivity(response.data);
                } else {
                    console.error('Error loading recent activity:', response.error);
                    showEmptyActivity();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading recent activity:', error);
                showEmptyActivity();
            },
            complete: function () {
                showChartLoading('activityRefresh', false);
            }
        });
    }

    function loadSystemHealth() {
        $.ajax({
            url: '/Security/Dashboard/GetSystemHealth',
            type: 'GET',
            beforeSend: function () {
                showChartLoading('healthRefresh', true);
            },
            success: function (response) {
                if (response.success) {
                    updateSystemHealth(response.data);
                } else {
                    console.error('Error loading system health:', response.error);
                    showHealthError();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading system health:', error);
                showHealthError();
            },
            complete: function () {
                showChartLoading('healthRefresh', false);
            }
        });
    }

    function updateStatistics(data) {
        // Update user statistics
        animateNumber('#totalUsers', data.users.total);
        animateNumber('#usersWithRoles', data.users.withRoles);
        $('#usersPercentage').text(data.users.percentage.toFixed(1));

        // Update role statistics
        animateNumber('#totalRoles', data.roles.total);
        animateNumber('#activeRoles', data.roles.active);
        $('#rolesUtilization').text(data.roles.utilization.toFixed(1));

        // Update permission statistics
        animateNumber('#totalPermissions', data.permissions.total);
        animateNumber('#activePermissions', data.permissions.active);
        $('#permissionsUtilization').text(data.permissions.utilization.toFixed(1));

        // Update permission type statistics
        animateNumber('#totalPermissionTypes', data.permissionTypes.total);
        animateNumber('#activePermissionTypes', data.permissionTypes.active);
        $('#permissionTypesUtilization').text(data.permissionTypes.utilization.toFixed(1));

        // Update last refreshed time
        $('#lastUpdated').text(new Date().toLocaleString());
    }

    function updatePermissionDistributionChart(data) {
        var series = data.map(item => item.count);
        var labels = data.map(item => item.name);
        var colors = data.map(item => item.color);

        charts.permissionDistribution.updateOptions({
            series: series,
            labels: labels,
            colors: colors.length > 0 ? colors : settings.chartColors
        });
    }

    function updateRoleDistribution(data) {
        var html = '';

        if (data.length === 0) {
            html = '<div class="text-center py-4 text-muted">No roles configured</div>';
        } else {
            data.forEach(function (role) {
                html += `
                    <div class="role-distribution-item">
                        <div class="d-flex align-items-center">
                            <span class="permission-badge" style="background-color: ${role.color}; color: white;">
                                ${role.name}
                            </span>
                            ${role.isSystemRole ? '<small class="badge bg-warning ms-1">System</small>' : ''}
                        </div>
                        <div class="text-end">
                            <div><strong>${role.userCount}</strong> <small class="text-muted">users</small></div>
                            <div><small class="text-info">${role.permissionCount} permissions</small></div>
                        </div>
                    </div>
                `;
            });
        }

        $('#roleDistributionContainer').html(html);
    }

    function updateRecentActivity(data) {
        var html = '';

        if (data.length === 0) {
            html = '<div class="text-center py-4 text-muted">No recent activities</div>';
        } else {
            data.forEach(function (activity) {
                html += `
                    <div class="activity-item">
                        <div class="activity-icon" style="background-color: ${activity.color};">
                            <i class="fa ${activity.icon}"></i>
                        </div>
                        <div class="flex-grow-1">
                            <div class="fw-bold">${activity.activity}</div>
                            <div class="text-muted small">
                                by ${activity.user}
                                <span class="text-primary">${activity.timeAgo}</span>
                            </div>
                        </div>
                    </div>
                `;
            });
        }

        $('#recentActivityContainer').html(html);
    }

    function updateSystemHealth(data) {
        var healthScore = data.healthScore;
        var healthColor = data.color;
        var healthStatus = data.status;

        // Update health score with animation
        animateNumber('#healthScore', healthScore, '%');

        // Update health indicator color
        $('#healthIndicator').css('background', `conic-gradient(${healthColor} ${healthScore}%, #e9ecef 0%)`);
        $('#healthStatus').text(healthStatus).css('color', healthColor);

        // Update recommendations
        var recommendationsHtml = '';
        if (data.recommendations && data.recommendations.length > 0) {
            recommendationsHtml = '<div class="mt-3"><h6>Recommendations:</h6><ul class="list-unstyled small">';
            data.recommendations.forEach(function (rec) {
                recommendationsHtml += `<li class="mb-1"><i class="fa fa-lightbulb text-warning me-1"></i>${rec}</li>`;
            });
            recommendationsHtml += '</ul></div>';
        }

        $('#healthRecommendations').html(recommendationsHtml);
    }

    function showEmptyPermissionChart() {
        charts.permissionDistribution.updateOptions({
            series: [1],
            labels: ['No Data'],
            colors: ['#e9ecef']
        });
    }

    function showEmptyRoleDistribution() {
        $('#roleDistributionContainer').html('<div class="text-center py-4 text-muted">No role data available</div>');
    }

    function showEmptyActivity() {
        $('#recentActivityContainer').html('<div class="text-center py-4 text-muted">No recent activities</div>');
    }

    function showHealthError() {
        $('#healthScore').text('--');
        $('#healthStatus').text('Error loading').css('color', '#dc3545');
        $('#healthRecommendations').html('<div class="alert alert-danger small">Failed to load health metrics</div>');
    }

    function showStatisticsLoading(show) {
        // Could add loading indicators to stats widgets if needed
    }

    function showChartLoading(elementId, show) {
        var icon = $('#' + elementId);
        if (show) {
            icon.addClass('fa-spin');
        } else {
            icon.removeClass('fa-spin');
        }
    }

    function animateNumber(selector, targetValue, suffix = '') {
        var element = $(selector);
        var currentValue = parseInt(element.text()) || 0;
        var increment = (targetValue - currentValue) / 30;

        var counter = 0;
        var timer = setInterval(function () {
            counter++;
            currentValue += increment;

            if (counter >= 30) {
                currentValue = targetValue;
                clearInterval(timer);
            }

            element.text(Math.round(currentValue) + suffix);
        }, 30);
    }

    function setupEventHandlers() {
        // Global refresh button
        window.refreshDashboard = function () {
            var refreshIcon = $('#refreshIcon');
            refreshIcon.addClass('fa-spin');

            loadAllData();

            setTimeout(function () {
                refreshIcon.removeClass('fa-spin');
                alert.success('Dashboard refreshed successfully!', { popup: false });
            }, 1000);
        };

        // Individual chart refresh functions
        window.loadPermissionDistribution = function () {
            loadPermissionDistribution();
        };

        window.loadRoleDistribution = function () {
            loadRoleDistribution();
        };

        window.loadRecentActivity = function () {
            loadRecentActivity();
        };

        window.loadSystemHealth = function () {
            loadSystemHealth();
        };

        // Handle window resize for chart responsiveness
        $(window).on('resize', function () {
            if (charts.permissionDistribution) {
                charts.permissionDistribution.resize();
            }
        });
    }

    function startAutoRefresh() {
        if (refreshInterval) {
            clearInterval(refreshInterval);
        }

        refreshInterval = setInterval(function () {
            console.log('Auto-refreshing dashboard data...');
            loadAllData();
        }, settings.refreshInterval);
    }

    function stopAutoRefresh() {
        if (refreshInterval) {
            clearInterval(refreshInterval);
            refreshInterval = null;
        }
    }

    // Public API
    return {
        init: init,
        loadAllData: loadAllData,
        refreshDashboard: function () { window.refreshDashboard(); },
        startAutoRefresh: startAutoRefresh,
        stopAutoRefresh: stopAutoRefresh,
        settings: settings
    };
})();

// Initialize when document is ready
$(document).ready(function () {
    if (typeof Dashboard !== 'undefined') {
        Dashboard.init();
    }
});