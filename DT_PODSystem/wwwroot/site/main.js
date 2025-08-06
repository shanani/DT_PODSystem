// Enhanced Gritter notification tracking for duplicate prevention
let activeGritterNotifications = new Map(); // Track active notifications with counters

function showNotification(message, options = {}) {
    const defaultOptions = {
        type: 'info',
        title: '',
        class_name: 'gritter-light',
        time: 5000,
        popup: true,
        sticky: false
    };
    const { type, title, time, popup, sticky } = { ...defaultOptions, ...options };

    // Use SweetAlert if flag is true
    if (popup && typeof swal !== 'undefined') {
        const finalTitle = title || (type === 'success' ? 'Success!'
            : type === 'warning' ? 'Warning!'
                : type === 'error' ? 'Error!'
                    : type === 'info' ? 'Notification'
                        : 'Notification');

        // Map icon types correctly
        const iconType = type === 'normal' ? 'info' : type;

        if (sticky) {
            // For sticky alerts, use more restrictive configuration
            const alertConfig = {
                title: finalTitle,
                text: message,
                icon: iconType,
                buttons: {
                    confirm: {
                        text: "OK",
                        value: true,
                        visible: true,
                        className: "btn btn-primary",
                        closeModal: true
                    }
                },
                closeOnClickOutside: false,
                closeOnEsc: false,
                allowOutsideClick: false,
                allowEscapeKey: false,
                backdrop: 'static',
                dangerMode: false,
                // Force user to click OK
                timer: null,
                timerProgressBar: false
            };
            alertQueue.push(alertConfig);
        } else {
            // For non-sticky alerts, show button always but allow normal behavior
            const alertConfig = {
                title: finalTitle,
                text: message,
                icon: iconType,
                timer: time > 0 ? time : undefined,
                buttons: {
                    confirm: {
                        text: "OK",
                        value: true,
                        visible: true, // ✅ Always show button
                        className: "btn btn-primary",
                        closeModal: true
                    }
                },
                closeOnClickOutside: true,
                closeOnEsc: true,
                allowOutsideClick: true,
                allowEscapeKey: true
            };
            alertQueue.push(alertConfig);
        }
        showQueuedAlert();
        return;
    }

    // Enhanced Gritter logic with duplicate prevention
    showGritterNotification(message, type, title, time, sticky);
}

function showGritterNotification(message, type, title, time, sticky) {
    let finalTitle = title || (type.toLowerCase() === 'success' ? 'Success!'
        : type.toLowerCase() === 'warning' ? 'Warning!'
            : type.toLowerCase() === 'error' ? 'Error!'
                : type.toLowerCase() === 'info' ? 'Notification'
                    : 'Notification');

    // Handle sticky behavior for Gritter
    let finalTime = sticky ? 0 : ((time !== undefined) ? time * 1000 : 5000);

    if (!sticky) {
        if (type.toLowerCase() === 'success') {
            finalTime = 3000;
        } else if (type.toLowerCase() === 'error') {
            finalTime = 0; // Errors are sticky by default
        } else if (type.toLowerCase() === 'warning') {
            finalTime = 5000;
        } else if (type.toLowerCase() === 'info') {
            finalTime = 5000;
        }

        if (time === 0) {
            finalTime = 0;
        }
    }

    let finalClass = 'gritter-light';
    if (type.toLowerCase() !== 'info') {
        finalClass += ` gritter-${type.toLowerCase()}`;
    }

    // Create unique key for this notification
    const notificationKey = `${type.toLowerCase()}-${finalTitle}-${message}`;

    // Check if identical notification already exists
    if (activeGritterNotifications.has(notificationKey)) {
        // Increment counter and update existing notification
        const existingNotification = activeGritterNotifications.get(notificationKey);
        existingNotification.count++;

        // Find and update the existing counter without changing the message content
        const gritterElement = $(`#gritter-item-${existingNotification.gritterId}`);

        if (gritterElement.length) {
            // Remove existing counter if any
            gritterElement.find('.notification-counter').remove();

            // Add counter badge to the notification wrapper (not the content)
            const counterBadge = `<span class="notification-counter badge bg-primary rounded-pill">${existingNotification.count}</span>`;
            gritterElement.find('.gritter-item').append(counterBadge);

            // Add pulse animation to the counter
            const counterElement = gritterElement.find('.notification-counter');
            counterElement.addClass('pulse-animation');
            setTimeout(() => {
                counterElement.removeClass('pulse-animation');
            }, 300);

            // Add counter styles if not already added
            if (!$('#notification-counter-styles').length) {
                $('head').append(`
                    <style id="notification-counter-styles">
                        .notification-counter {
                            position: absolute !important;
                            top: 8px !important;
                            right: 8px !important;
                            font-size: 0.7rem !important;
                            font-weight: bold !important;
                            min-width: 18px !important;
                            height: 18px !important;
                            line-height: 18px !important;
                            text-align: center !important;
                            display: flex !important;
                            align-items: center !important;
                            justify-content: center !important;
                            z-index: 1000 !important;
                            border: 1px solid rgba(255,255,255,0.3) !important;
                        }
                        
                        .pulse-animation {
                            animation: notificationPulse 0.3s ease-in-out !important;
                        }
                        
                        @keyframes notificationPulse {
                            0% { transform: scale(1); }
                            50% { transform: scale(1.3); }
                            100% { transform: scale(1); }
                        }
                        
                        .gritter-item {
                            position: relative !important;
                        }
                        
                        .notification-with-counter .gritter-item {
                            border-left: 4px solid #007bff !important;
                        }
                        
                        .notification-with-counter.gritter-success .gritter-item {
                            border-left: 4px solid #28a745 !important;
                        }
                        
                        .notification-with-counter.gritter-warning .gritter-item {
                            border-left: 4px solid #ffc107 !important;
                        }
                        
                        .notification-with-counter.gritter-error .gritter-item {
                            border-left: 4px solid #dc3545 !important;
                        }
                        
                        .gritter-close {
                            z-index: 1001 !important;
                        }
                    </style>
                `);
            }
        }

        // Reset timer if it's not sticky
        if (!sticky && finalTime > 0) {
            // Clear existing timer if any
            if (existingNotification.timer) {
                clearTimeout(existingNotification.timer);
            }

            // Set new timer
            existingNotification.timer = setTimeout(() => {
                $.gritter.remove(existingNotification.gritterId);
                activeGritterNotifications.delete(notificationKey);
            }, finalTime);
        }

    } else {
        // Create new notification
        const gritterId = $.gritter.add({
            title: finalTitle,
            text: message,
            sticky: sticky || finalTime === 0,
            time: finalTime,
            class_name: finalClass,
            after_open: function () {
                // Add counter styles when notification opens (for new notifications that might get counters later)
                if (!$('#notification-counter-styles').length) {
                    $('head').append(`
                        <style id="notification-counter-styles">
                            .notification-counter {
                                position: absolute !important;
                                top: 8px !important;
                                right: 8px !important;
                                font-size: 0.7rem !important;
                                font-weight: bold !important;
                                min-width: 18px !important;
                                height: 18px !important;
                                line-height: 18px !important;
                                text-align: center !important;
                                display: flex !important;
                                align-items: center !important;
                                justify-content: center !important;
                                z-index: 1000 !important;
                                border: 1px solid rgba(255,255,255,0.3) !important;
                            }
                            
                            .pulse-animation {
                                animation: notificationPulse 0.3s ease-in-out !important;
                            }
                            
                            @keyframes notificationPulse {
                                0% { transform: scale(1); }
                                50% { transform: scale(1.3); }
                                100% { transform: scale(1); }
                            }
                            
                            .gritter-item {
                                position: relative !important;
                            }
                            
                            .notification-with-counter .gritter-item {
                                border-left: 4px solid #007bff !important;
                            }
                            
                            .notification-with-counter.gritter-success .gritter-item {
                                border-left: 4px solid #28a745 !important;
                            }
                            
                            .notification-with-counter.gritter-warning .gritter-item {
                                border-left: 4px solid #ffc107 !important;
                            }
                            
                            .notification-with-counter.gritter-error .gritter-item {
                                border-left: 4px solid #dc3545 !important;
                            }
                            
                            .gritter-close {
                                z-index: 1001 !important;
                            }
                        </style>
                    `);
                }
            },
            after_close: function () {
                // Clean up when notification is closed
                activeGritterNotifications.delete(notificationKey);
            }
        });

        // Store notification info
        const notificationInfo = {
            gritterId: gritterId,
            count: 1,
            timer: null
        };

        // Set timer for cleanup if not sticky
        if (!sticky && finalTime > 0) {
            notificationInfo.timer = setTimeout(() => {
                activeGritterNotifications.delete(notificationKey);
            }, finalTime);
        }

        activeGritterNotifications.set(notificationKey, notificationInfo);
    }
}

// Alert queue system for SweetAlert
let alertQueue = [];
let isShowingAlert = false;

function showQueuedAlert() {
    if (alertQueue.length === 0 || isShowingAlert) return;
    isShowingAlert = true;
    const alertData = alertQueue.shift();
    swal(alertData).then(() => {
        isShowingAlert = false;
        showQueuedAlert(); // Show next alert
    });
}

// ✅ FIXED: Store original alert FIRST, before any overrides
const originalAlert = window.alert;

// ✅ FIXED: Override window.alert IMMEDIATELY as a function
window.alert = function (message, options = { sticky: true }) {
    // For simple string messages (like DataTables uses), use info
    if (typeof message === 'string' && arguments.length === 1) {
        return showNotification(message, { type: 'info', popup: false, ...options });
    }

    // If our custom system isn't available, fallback to original
    if (originalAlert && typeof originalAlert === 'function') {
        return originalAlert.call(this, message);
    }

    // Last resort fallback
    console.log('Alert:', message);
};

// ✅ FIXED: Add custom methods directly to window.alert with sticky support
window.alert.success = function (message, options = {}) {
    showNotification(message, { ...options, type: 'success' });
};

window.alert.warning = function (message, options = {}) {
    showNotification(message, { ...options, type: 'warning' });
};

window.alert.info = function (message, options = {}) {
    showNotification(message, { ...options, type: 'info' });
};

window.alert.error = function (message, options = {}) {
    // Errors are sticky by default
    showNotification(message, { sticky: true, ...options, type: 'error' });
};

// ✅ NEW: Add sticky versions of each method
window.alert.successSticky = function (message, options = {}) {
    showNotification(message, { ...options, type: 'success', sticky: true });
};

window.alert.warningSticky = function (message, options = {}) {
    showNotification(message, { ...options, type: 'warning', sticky: true });
};

window.alert.infoSticky = function (message, options = {}) {
    showNotification(message, { ...options, type: 'info', sticky: true });
};

window.alert.errorSticky = function (message, options = {}) {
    showNotification(message, { ...options, type: 'error', sticky: true });
};

// ✅ FIXED: Create alias for easier access (optional)
const alert = window.alert;

// Preserve original alert function if needed
window.alert.native = originalAlert;

// Enhanced utility functions
window.alert.clearAll = function () {
    // Clear all Gritter notifications
    $.gritter.removeAll();
    // Clear tracking map
    activeGritterNotifications.clear();
    // Clear SweetAlert queue
    alertQueue = [];
    isShowingAlert = false;
};

window.alert.clearType = function (type) {
    // Clear notifications of specific type
    activeGritterNotifications.forEach((notification, key) => {
        if (key.startsWith(type.toLowerCase() + '-')) {
            $.gritter.remove(notification.gritterId);
            activeGritterNotifications.delete(key);
        }
    });
};

window.alert.getActiveCount = function (type = null) {
    if (type) {
        let count = 0;
        activeGritterNotifications.forEach((notification, key) => {
            if (key.startsWith(type.toLowerCase() + '-')) {
                count += notification.count;
            }
        });
        return count;
    }

    let totalCount = 0;
    activeGritterNotifications.forEach(notification => {
        totalCount += notification.count;
    });
    return totalCount;
};

// Handle TempData notifications from server-side
$(document).ready(function () {
    if (typeof window.tempDataAlert !== 'undefined' && window.tempDataAlert) {
        const { message, type, title, time, popup } = window.tempDataAlert;
        showNotification(message, { type, title, time, popup });
        window.tempDataAlert = null;
    }
});

// Global AJAX error handler
$(document).ajaxError(function (event, xhr, settings, thrownError) {
    if (xhr.status === 404) {
        alert.error('The requested resource was not found.');
    } else if (xhr.status === 500) {
        alert.error('An internal server error occurred. Please try again.');
    } else if (xhr.status === 403) {
        alert.warning('You do not have permission to perform this action.');
    } else if (xhr.status !== 0) { // Ignore aborted requests
        alert.error('An error occurred while processing your request.');
    }
});


$(document).ready(function () {
    // Override Gritter's default settings for all notifications
    if (typeof $.gritter !== 'undefined') {
        $.extend($.gritter.options, {
            fade_in_speed: 200,    // Fast slide in
            fade_out_speed: 100,   // Super fast slide out
            time: 3000,            // Default display time
             click_dismiss: true  // That's it! Just one option
        });
    }
})