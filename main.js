// KC868-A8 Smart Controller Web Interface
// Main JavaScript file

// Global variables
const apiBase = '/api';
let relays = [];
let inputs = [];
let analogInputs = [];
let schedules = [];
let automationRules = [];
let updateInterval;

// Document ready function
document.addEventListener('DOMContentLoaded', function () {
    // Initialize the application
    init();

    // Set up event listeners
    setupEventListeners();

    // Start periodic updates
    startUpdates();
});

// Initialize the application
function init() {
    // Load initial data
    loadDeviceStatus();
    loadRelays();
    loadInputs();
    loadAnalogInputs();
    loadSchedules();
    loadAutomationRules();
    loadSettings();

    // Initialize the first condition in the automation rule modal
    addCondition();
}

// Start periodic updates
function startUpdates() {
    // Update data every 2 seconds
    updateInterval = setInterval(() => {
        loadDeviceStatus();
        loadRelays();
        loadInputs();
        loadAnalogInputs();
    }, 2000);
}

// Setup event listeners
function setupEventListeners() {
    // Navigation events
    document.querySelectorAll('.nav-link').forEach(tab => {
        tab.addEventListener('click', function () {
            // Specific actions when changing tabs
            if (tab.id === 'schedules-tab') {
                loadSchedules();
            } else if (tab.id === 'automation-tab') {
                loadAutomationRules();
            } else if (tab.id === 'io-settings-tab') {
                loadIOSettings();
            } else if (tab.id === 'system-settings-tab') {
                loadSettings();
            }
        });
    });

    // Relay controls
    document.getElementById('allOn').addEventListener('click', () => setAllRelays(true));
    document.getElementById('allOff').addEventListener('click', () => setAllRelays(false));

    // System control buttons
    document.getElementById('rebootBtn').addEventListener('click', rebootDevice);
    document.getElementById('factoryResetBtn').addEventListener('click', factoryReset);

    // Settings form events
    document.getElementById('useDhcp').addEventListener('change', function () {
        document.getElementById('staticIpSettings').style.display = this.checked ? 'none' : 'block';
    });

    document.getElementById('useEthernet').addEventListener('change', function () {
        document.getElementById('wifiSettings').style.display = this.checked ? 'none' : 'block';
    });

    document.getElementById('mqttEnabled').addEventListener('change', function () {
        document.getElementById('mqttSettings').style.display = this.checked ? 'block' : 'none';
    });

    document.getElementById('alexaEnabled').addEventListener('change', function () {
        document.getElementById('alexaSettings').style.display = this.checked ? 'block' : 'none';
    });

    document.getElementById('usbEnabled').addEventListener('change', function () {
        document.getElementById('usbSettings').style.display = this.checked ? 'block' : 'none';
    });

    document.querySelector('input[name="timeSource"]').addEventListener('change', function () {
        document.getElementById('manualTimeControls').style.display =
            document.getElementById('manualTime').checked ? 'block' : 'none';
    });

    // Settings form submissions
    document.getElementById('networkSettingsForm').addEventListener('submit', saveNetworkSettings);
    document.getElementById('deviceSettingsForm').addEventListener('submit', saveDeviceSettings);
    document.getElementById('mqttSettingsForm').addEventListener('submit', saveMqttSettings);
    document.getElementById('alexaSettingsForm').addEventListener('submit', saveAlexaSettings);
    document.getElementById('usbSettingsForm').addEventListener('submit', saveUsbSettings);
    document.getElementById('relaySettingsForm').addEventListener('submit', saveRelaySettings);
    document.getElementById('inputSettingsForm').addEventListener('submit', saveInputSettings);
    document.getElementById('analogSettingsForm').addEventListener('submit', saveAnalogSettings);

    // Alexa discovery button
    document.getElementById('discoverDevicesBtn').addEventListener('click', triggerAlexaDiscovery);

    // Automation modal events
    document.getElementById('addRuleBtn').addEventListener('click', showAddAutomationModal);
    document.getElementById('saveRuleBtn').addEventListener('click', saveAutomationRule);
    document.getElementById('addConditionBtn').addEventListener('click', addCondition);
    document.getElementById('useTimer').addEventListener('change', function () {
        document.getElementById('timerSettingsContainer').style.display = this.checked ? 'block' : 'none';
    });
    document.getElementById('actionType').addEventListener('change', updateActionContainers);

    // Schedule modal events
    document.getElementById('addScheduleBtn').addEventListener('click', showAddScheduleModal);
    document.getElementById('saveScheduleBtn').addEventListener('click', saveSchedule);
    document.getElementById('scheduleActionType').addEventListener('change', updateScheduleActionContainers);
}

// ======== API Functions ========

// Load device status
function loadDeviceStatus() {
    fetch(`${apiBase}/status`)
        .then(response => response.json())
        .then(data => {
            // Update connection status
            const statusElement = document.getElementById('connectionStatus');
            if (data.connection !== 'disconnected') {
                statusElement.textContent = `Connected (${data.connection})`;
                statusElement.classList.add('connected');
            } else {
                statusElement.textContent = 'Disconnected';
                statusElement.classList.remove('connected');
            }

            // Update status information
            document.getElementById('deviceName').textContent = data.device || 'KC868-A8 Controller';
            document.getElementById('ipAddress').textContent = data.ip || 'Not Available';
            document.getElementById('connectionType').textContent = data.connection || 'Disconnected';
            document.getElementById('currentTime').textContent = data.time || 'Not Synchronized';
            document.getElementById('uptime').textContent = formatUptime(data.uptime);
            document.getElementById('usbStatus').textContent = data.usb ? 'Enabled' : 'Disabled';
        })
        .catch(error => console.error('Error loading device status:', error));
}

// Load relay information
function loadRelays() {
    fetch(`${apiBase}/relay`)
        .then(response => response.json())
        .then(data => {
            relays = data.relays;
            updateRelayUI();
        })
        .catch(error => console.error('Error loading relays:', error));
}

// Load input information
function loadInputs() {
    fetch(`${apiBase}/input`)
        .then(response => response.json())
        .then(data => {
            inputs = data.inputs;
            updateInputUI();
        })
        .catch(error => console.error('Error loading inputs:', error));
}

// Load analog input information
function loadAnalogInputs() {
    fetch(`${apiBase}/analog`)
        .then(response => response.json())
        .then(data => {
            analogInputs = data.analogInputs;
            updateAnalogUI();
        })
        .catch(error => console.error('Error loading analog inputs:', error));
}

// Load schedules
function loadSchedules() {
    fetch(`${apiBase}/schedules`)
        .then(response => response.json())
        .then(data => {
            schedules = data.schedules;
            updateSchedulesUI();
        })
        .catch(error => console.error('Error loading schedules:', error));
}

// Load automation rules
function loadAutomationRules() {
    fetch(`${apiBase}/automation`)
        .then(response => response.json())
        .then(data => {
            automationRules = data.rules;
            updateAutomationUI();
        })
        .catch(error => console.error('Error loading automation rules:', error));
}

// Load I/O settings
function loadIOSettings() {
    Promise.all([
        fetch(`${apiBase}/relay`).then(response => response.json()),
        fetch(`${apiBase}/input`).then(response => response.json()),
        fetch(`${apiBase}/analog`).then(response => response.json())
    ])
        .then(([relayData, inputData, analogData]) => {
            relays = relayData.relays;
            inputs = inputData.inputs;
            analogInputs = analogData.analogInputs;

            updateIOSettingsUI();
        })
        .catch(error => console.error('Error loading I/O settings:', error));
}

// Load system settings
function loadSettings() {
    fetch(`${apiBase}/settings`)
        .then(response => response.json())
        .then(data => {
            updateSettingsUI(data);
        })
        .catch(error => console.error('Error loading settings:', error));
}

// Toggle a relay
function toggleRelay(relayId) {
    const relay = relays.find(r => r.id === relayId);
    if (!relay) return;

    const newState = !relay.state;
    const action = newState ? 'on' : 'off';

    fetch(`${apiBase}/relay/${relayId}/${action}`)
        .then(response => response.json())
        .then(() => {
            // Update the relay state in our local data
            relay.state = newState;
            updateRelayUI();
        })
        .catch(error => console.error(`Error toggling relay ${relayId}:`, error));
}

// Set all relays
function setAllRelays(state) {
    const action = state ? 'on' : 'off';

    fetch(`${apiBase}/relay/all/${action}`)
        .then(response => response.json())
        .then(() => {
            // Update all relays in our local data
            relays.forEach(relay => relay.state = state);
            updateRelayUI();
        })
        .catch(error => console.error(`Error setting all relays to ${action}:`, error));
}

// Toggle automation rule enabled state
function toggleAutomationRule(ruleId) {
    const rule = automationRules.find(r => r.id === ruleId);
    if (!rule) return;

    const newState = !rule.enabled;

    fetch(`${apiBase}/automation/${ruleId}/${newState ? 'enable' : 'disable'}`)
        .then(response => response.json())
        .then(() => {
            // Update the rule state in our local data
            rule.enabled = newState;
            updateAutomationUI();
        })
        .catch(error => console.error(`Error toggling automation rule ${ruleId}:`, error));
}

// Toggle schedule enabled state
function toggleSchedule(scheduleId) {
    const schedule = schedules.find(s => s.id === scheduleId);
    if (!schedule) return;

    const newState = !schedule.enabled;

    fetch(`${apiBase}/schedule/${scheduleId}/${newState ? 'enable' : 'disable'}`)
        .then(response => response.json())
        .then(() => {
            // Update the schedule state in our local data
            schedule.enabled = newState;
            updateSchedulesUI();
        })
        .catch(error => console.error(`Error toggling schedule ${scheduleId}:`, error));
}

// Delete automation rule
function deleteAutomationRule(ruleId) {
    if (!confirm('Are you sure you want to delete this automation rule?')) {
        return;
    }

    // Get updated rules list with this one removed
    const updatedRules = automationRules.filter(r => r.id !== ruleId);

    // Send the full list without the deleted rule
    fetch(`${apiBase}/automation`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ rules: updatedRules })
    })
        .then(response => response.json())
        .then(() => {
            // Reload the rules list
            loadAutomationRules();
        })
        .catch(error => console.error(`Error deleting automation rule ${ruleId}:`, error));
}

// Delete schedule
function deleteSchedule(scheduleId) {
    if (!confirm('Are you sure you want to delete this schedule?')) {
        return;
    }

    // Get updated schedules list with this one removed
    const updatedSchedules = schedules.filter(s => s.id !== scheduleId);

    // Send the full list without the deleted schedule
    fetch(`${apiBase}/schedules`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ schedules: updatedSchedules })
    })
        .then(response => response.json())
        .then(() => {
            // Reload the schedules list
            loadSchedules();
        })
        .catch(error => console.error(`Error deleting schedule ${scheduleId}:`, error));
}

// Reboot the device
function rebootDevice() {
    if (!confirm('Are you sure you want to reboot the device?')) {
        return;
    }

    fetch(`${apiBase}/reboot`)
        .then(response => response.json())
        .then(() => {
            alert('Device is rebooting. The page will refresh in 10 seconds.');
            setTimeout(() => {
                window.location.reload();
            }, 10000);
        })
        .catch(error => console.error('Error rebooting device:', error));
}

// Factory reset the device
function factoryReset() {
    if (!confirm('WARNING: This will reset ALL settings to factory defaults! Are you sure?')) {
        return;
    }

    if (!confirm('ALL your configuration will be lost. This cannot be undone. Continue?')) {
        return;
    }

    fetch(`${apiBase}/factory-reset`)
        .then(response => response.json())
        .then(() => {
            alert('Factory reset initiated. The device will reboot. The page will refresh in 15 seconds.');
            setTimeout(() => {
                window.location.reload();
            }, 15000);
        })
        .catch(error => console.error('Error factory resetting device:', error));
}

// Trigger Alexa discovery
function triggerAlexaDiscovery() {
    fetch(`${apiBase}/alexa/discover`)
        .then(response => response.json())
        .then(data => {
            alert(data.message);
        })
        .catch(error => console.error('Error triggering Alexa discovery:', error));
}

// ======== Settings Functions ========

// Save network settings
function saveNetworkSettings(event) {
    event.preventDefault();

    const settings = {
        useEthernet: document.getElementById('useEthernet').checked,
        ssid: document.getElementById('wifiSsid').value,
        password: document.getElementById('wifiPassword').value,
        dhcp: document.getElementById('useDhcp').checked,
        ip: document.getElementById('staticIp').value,
        gateway: document.getElementById('gateway').value,
        subnet: document.getElementById('subnet').value,
        dns: document.getElementById('dns').value
    };

    fetch(`${apiBase}/settings/network`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(settings)
    })
        .then(response => response.json())
        .then(data => {
            alert(data.message);

            if (data.restart) {
                alert('Device is restarting with new network settings. Please reconnect if necessary.');
                setTimeout(() => {
                    window.location.reload();
                }, 10000);
            }
        })
        .catch(error => console.error('Error saving network settings:', error));
}

// Save device settings
function saveDeviceSettings(event) {
    event.preventDefault();

    const settings = {
        name: document.getElementById('deviceName').value,
        autoTime: document.getElementById('autoTime').checked
    };

    if (document.getElementById('manualTime').checked) {
        settings.manualTime = document.getElementById('manualDateTime').value.replace('T', ' ');
    }

    fetch(`${apiBase}/settings/device`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(settings)
    })
        .then(response => response.json())
        .then(data => {
            alert(data.message);
            loadDeviceStatus();
        })
        .catch(error => console.error('Error saving device settings:', error));
}

// Save MQTT settings
function saveMqttSettings(event) {
    event.preventDefault();

    const settings = {
        enabled: document.getElementById('mqttEnabled').checked,
        server: document.getElementById('mqttServer').value,
        port: parseInt(document.getElementById('mqttPort').value),
        username: document.getElementById('mqttUser').value,
        password: document.getElementById('mqttPass').value
    };

    fetch(`${apiBase}/settings`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ mqtt: settings })
    })
        .then(response => response.json())
        .then(data => {
            alert(data.message);
        })
        .catch(error => console.error('Error saving MQTT settings:', error));
}

// Save Alexa settings
function saveAlexaSettings(event) {
    event.preventDefault();

    const settings = {
        enabled: document.getElementById('alexaEnabled').checked,
        deviceName: document.getElementById('alexaDeviceName').value
    };

    fetch(`${apiBase}/settings/alexa`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(settings)
    })
        .then(response => response.json())
        .then(data => {
            alert(data.message);
        })
        .catch(error => console.error('Error saving Alexa settings:', error));
}

// Save USB settings
function saveUsbSettings(event) {
    event.preventDefault();

    const settings = {
        enabled: document.getElementById('usbEnabled').checked,
        baudRate: parseInt(document.getElementById('baudRate').value)
    };

    fetch(`${apiBase}/settings/usb`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(settings)
    })
        .then(response => response.json())
        .then(data => {
            alert(data.message);
            loadDeviceStatus();
        })
        .catch(error => console.error('Error saving USB settings:', error));
}

// Save relay settings
function saveRelaySettings(event) {
    event.preventDefault();

    const relaySettings = [];
    const container = document.getElementById('relaySettingsContainer');

    // Collect settings from form
    container.querySelectorAll('.relay-setting').forEach(setting => {
        const id = parseInt(setting.getAttribute('data-relay-id'));
        relaySettings.push({
            id: id,
            name: setting.querySelector('.relay-name').value,
            invertLogic: setting.querySelector('.relay-invert').checked,
            rememberState: setting.querySelector('.relay-remember').checked
        });
    });

    fetch(`${apiBase}/settings/io`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ relays: relaySettings })
    })
        .then(response => response.json())
        .then(data => {
            alert(data.message);
            loadRelays();
        })
        .catch(error => console.error('Error saving relay settings:', error));
}

// Save input settings
function saveInputSettings(event) {
    event.preventDefault();

    const inputSettings = [];
    const container = document.getElementById('inputSettingsContainer');

    // Collect settings from form
    container.querySelectorAll('.input-setting').forEach(setting => {
        const id = parseInt(setting.getAttribute('data-input-id'));
        inputSettings.push({
            id: id,
            name: setting.querySelector('.input-name').value,
            invertLogic: setting.querySelector('.input-invert').checked,
            mode: setting.querySelector('.input-mode').value
        });
    });

    fetch(`${apiBase}/settings/io`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ inputs: inputSettings })
    })
        .then(response => response.json())
        .then(data => {
            alert(data.message);
            loadInputs();
        })
        .catch(error => console.error('Error saving input settings:', error));
}

// Save analog input settings
function saveAnalogSettings(event) {
    event.preventDefault();

    const analogSettings = [];
    const container = document.getElementById('analogSettingsContainer');

    // Collect settings from form
    container.querySelectorAll('.analog-setting').forEach(setting => {
        const id = parseInt(setting.getAttribute('data-analog-id'));
        analogSettings.push({
            id: id,
            name: setting.querySelector('.analog-name').value,
            mode: setting.querySelector('.analog-mode').value,
            unit: setting.querySelector('.analog-unit').value
        });
    });

    fetch(`${apiBase}/settings/io`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ analogInputs: analogSettings })
    })
        .then(response => response.json())
        .then(data => {
            alert(data.message);
            loadAnalogInputs();
        })
        .catch(error => console.error('Error saving analog input settings:', error));
}

// ======== Automation Rule Functions ========

// Show add automation rule modal
function showAddAutomationModal() {
    // Reset the form
    document.getElementById('automationRuleForm').reset();
    document.getElementById('ruleId').value = '';
    document.getElementById('automationModalTitle').textContent = 'Add Automation Rule';

    // Clear existing conditions except the first one
    const container = document.getElementById('conditionsContainer');
    while (container.children.length > 0) {
        container.removeChild(container.lastChild);
    }

    // Add first condition
    addCondition();

    // Hide logic operator selector (only show when multiple conditions)
    document.getElementById('logicOperatorContainer').style.display = 'none';

    // Hide timer settings
    document.getElementById('timerSettingsContainer').style.display = 'none';

    // Populate selects with data
    populateRelaySelect('relayTarget');
    populateInputSelect();
    populateAnalogSelect();

    // Show relay action by default
    updateActionContainers();

    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('automationRuleModal'));
    modal.show();
}

// Show edit automation rule modal
function showEditAutomationModal(ruleId) {
    const rule = automationRules.find(r => r.id === ruleId);
    if (!rule) return;

    // Set form title
    document.getElementById('automationModalTitle').textContent = 'Edit Automation Rule';
    document.getElementById('ruleId').value = ruleId;

    // Clear existing conditions
    const container = document.getElementById('conditionsContainer');
    while (container.children.length > 0) {
        container.removeChild(container.lastChild);
    }

    // Fill in basic fields
    document.getElementById('ruleName').value = rule.name;
    document.getElementById('ruleEnabled').checked = rule.enabled;

    // Populate selects with data
    populateRelaySelect('relayTarget');
    populateInputSelect();
    populateAnalogSelect();

    // Handle conditions
    if (rule.conditions && rule.conditions.length > 0) {
        // Add a condition element for each condition
        rule.conditions.forEach((condition, index) => {
            addCondition();

            // Get the last added condition element
            const conditionElements = document.querySelectorAll('.condition-group');
            const condElement = conditionElements[conditionElements.length - 1];

            // Set condition type
            const typeSelect = condElement.querySelector('.condition-type');
            typeSelect.value = condition.type;

            // Show/hide condition sections based on type
            if (condition.type === 'digital') {
                condElement.querySelector('.digital-condition').style.display = 'block';
                condElement.querySelector('.analog-condition').style.display = 'none';

                // Set digital input values
                condElement.querySelector('.digital-input').value = condition.sourceId;
                condElement.querySelector('.digital-state').value = condition.condition;
            } else if (condition.type === 'analog') {
                condElement.querySelector('.digital-condition').style.display = 'none';
                condElement.querySelector('.analog-condition').style.display = 'block';

                // Set analog input values
                condElement.querySelector('.analog-input').value = condition.sourceId;
                condElement.querySelector('.analog-condition-type').value = condition.condition;
                condElement.querySelector('.analog-threshold1').value = condition.threshold1;

                // Handle threshold2 for "between" condition
                if (condition.condition === 'between') {
                    condElement.querySelector('.threshold2-container').style.display = 'block';
                    condElement.querySelector('.analog-threshold2').value = condition.threshold2;
                } else {
                    condElement.querySelector('.threshold2-container').style.display = 'none';
                }
            }
        });

        // Set logic operator if multiple conditions
        if (rule.conditions.length > 1) {
            document.getElementById('logicOperatorContainer').style.display = 'block';
            document.getElementById('logicOperator').value = rule.logicOperator || 'AND';
        } else {
            document.getElementById('logicOperatorContainer').style.display = 'none';
        }
    } else {
        // Add a single default condition
        addCondition();
        document.getElementById('logicOperatorContainer').style.display = 'none';
    }

    // Set timer settings
    document.getElementById('useTimer').checked = rule.useTimer || false;
    document.getElementById('timerSettingsContainer').style.display = rule.useTimer ? 'block' : 'none';

    if (rule.useTimer) {
        document.getElementById('timerType').value = rule.timerType || 'ondelay';
        document.getElementById('timerDuration').value = rule.timerDuration || 1000;
    }

    // Set action settings
    document.getElementById('actionType').value = rule.action;
    updateActionContainers();

    if (rule.action === 'relay') {
        document.getElementById('relayTarget').value = rule.targetId;
        document.getElementById('relayState').value = rule.targetState;
    } else if (rule.action === 'scene') {
        document.getElementById('sceneTarget').value = rule.targetId;
    } else if (rule.action === 'notification') {
        document.getElementById('notificationMessage').value = rule.message;
    }

    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('automationRuleModal'));
    modal.show();
}

// Add condition to the automation rule
function addCondition() {
    // Clone the condition template
    const template = document.querySelector('.condition-template');
    const clone = template.cloneNode(true);
    clone.classList.remove('condition-template', 'd-none');

    // Add event listeners to the new condition
    addConditionEventListeners(clone);

    // Add the clone to the conditions container
    const container = document.getElementById('conditionsContainer');
    container.appendChild(clone);

    // Update condition numbering
    updateConditionNumbers();

    // Show logic operator if we have more than one condition
    document.getElementById('logicOperatorContainer').style.display =
        container.querySelectorAll('.condition-group').length > 1 ? 'block' : 'none';

    // Populate input and analog selects
    populateInputSelect();
    populateAnalogSelect();
}

// Add event listeners to a condition element
function addConditionEventListeners(conditionElement) {
    // Remove button
    conditionElement.querySelector('.btn-remove-condition').addEventListener('click', function () {
        const container = document.getElementById('conditionsContainer');

        // Don't remove if it's the last condition
        if (container.querySelectorAll('.condition-group').length <= 1) {
            return;
        }

        conditionElement.remove();

        // Update condition numbering
        updateConditionNumbers();

        // Hide logic operator if we only have one condition
        document.getElementById('logicOperatorContainer').style.display =
            container.querySelectorAll('.condition-group').length > 1 ? 'block' : 'none';
    });

    // Condition type change
    conditionElement.querySelector('.condition-type').addEventListener('change', function () {
        const type = this.value;

        if (type === 'digital') {
            conditionElement.querySelector('.digital-condition').style.display = 'block';
            conditionElement.querySelector('.analog-condition').style.display = 'none';
        } else if (type === 'analog') {
            conditionElement.querySelector('.digital-condition').style.display = 'none';
            conditionElement.querySelector('.analog-condition').style.display = 'block';
        }
    });

    // Analog condition type change
    conditionElement.querySelector('.analog-condition-type').addEventListener('change', function () {
        const type = this.value;
        const threshold2Container = conditionElement.querySelector('.threshold2-container');

        if (type === 'between') {
            threshold2Container.style.display = 'block';
        } else {
            threshold2Container.style.display = 'none';
        }
    });
}

// Update condition numbers
function updateConditionNumbers() {
    const conditions = document.querySelectorAll('.condition-group');
    conditions.forEach((condition, index) => {
        condition.querySelector('.condition-title').textContent = `Condition ${index + 1}`;
    });
}

// Update action containers based on selected action type
function updateActionContainers() {
    const actionType = document.getElementById('actionType').value;

    document.getElementById('relayActionContainer').style.display = 'none';
    document.getElementById('sceneActionContainer').style.display = 'none';
    document.getElementById('notificationActionContainer').style.display = 'none';

    if (actionType === 'relay') {
        document.getElementById('relayActionContainer').style.display = 'block';
    } else if (actionType === 'scene') {
        document.getElementById('sceneActionContainer').style.display = 'block';
    } else if (actionType === 'notification') {
        document.getElementById('notificationActionContainer').style.display = 'block';
    }
}

// Collect automation rule data from the form
function collectAutomationRuleData() {
    const ruleId = document.getElementById('ruleId').value;
    const ruleName = document.getElementById('ruleName').value;
    const enabled = document.getElementById('ruleEnabled').checked;

    // Collect conditions
    const conditions = [];
    document.querySelectorAll('.condition-group').forEach(condElement => {
        if (condElement.classList.contains('condition-template')) return;

        const type = condElement.querySelector('.condition-type').value;

        const condition = {
            type: type
        };

        if (type === 'digital') {
            condition.sourceId = parseInt(condElement.querySelector('.digital-input').value);
            condition.condition = condElement.querySelector('.digital-state').value;
        } else if (type === 'analog') {
            condition.sourceId = parseInt(condElement.querySelector('.analog-input').value);
            condition.condition = condElement.querySelector('.analog-condition-type').value;
            condition.threshold1 = parseInt(condElement.querySelector('.analog-threshold1').value);

            if (condition.condition === 'between') {
                condition.threshold2 = parseInt(condElement.querySelector('.analog-threshold2').value);
            }
        }

        conditions.push(condition);
    });

    // Logic operator
    const logicOperator = document.getElementById('logicOperator').value;

    // Timer settings
    const useTimer = document.getElementById('useTimer').checked;
    const timerType = document.getElementById('timerType').value;
    const timerDuration = parseInt(document.getElementById('timerDuration').value);

    // Action settings
    const actionType = document.getElementById('actionType').value;
    let targetId, targetState, message;

    if (actionType === 'relay') {
        targetId = parseInt(document.getElementById('relayTarget').value);
        targetState = document.getElementById('relayState').value;
    } else if (actionType === 'scene') {
        targetId = parseInt(document.getElementById('sceneTarget').value);
    } else if (actionType === 'notification') {
        message = document.getElementById('notificationMessage').value;
    }

    // Build the rule object
    const rule = {
        name: ruleName,
        enabled: enabled,
        conditions: conditions,
        logicOperator: logicOperator,
        useTimer: useTimer,
        action: actionType
    };

    // Only add ID if editing an existing rule
    if (ruleId) {
        rule.id = parseInt(ruleId);
    }

    // Add timer details if timer is enabled
    if (useTimer) {
        rule.timerType = timerType;
        rule.timerDuration = timerDuration;
    }

    // Add action details based on type
    if (actionType === 'relay') {
        rule.targetId = targetId;
        rule.targetState = targetState;
    } else if (actionType === 'scene') {
        rule.targetId = targetId;
    } else if (actionType === 'notification') {
        rule.message = message;
    }

    return rule;
}

// Save automation rule
function saveAutomationRule() {
    // Validate form
    const form = document.getElementById('automationRuleForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    // Collect data from form
    const rule = collectAutomationRuleData();

    // Find where to insert/update the rule
    let updatedRules;

    if (rule.id !== undefined) {
        // Updating existing rule
        updatedRules = automationRules.map(r => r.id === rule.id ? rule : r);
    } else {
        // Adding new rule
        // Assign next available ID
        const maxId = automationRules.length > 0 ?
            Math.max(...automationRules.map(r => r.id)) : -1;
        rule.id = maxId + 1;

        updatedRules = [...automationRules, rule];
    }

    // Send to the server
    fetch(`${apiBase}/automation`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ rules: updatedRules })
    })
        .then(response => response.json())
        .then(data => {
            // Close the modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('automationRuleModal'));
            modal.hide();

            // Reload automation rules
            loadAutomationRules();
        })
        .catch(error => console.error('Error saving automation rule:', error));
}

// ======== Schedule Functions ========

// Show add schedule modal
function showAddScheduleModal() {
    // Reset the form
    document.getElementById('scheduleForm').reset();
    document.getElementById('scheduleId').value = '';
    document.getElementById('scheduleModalTitle').textContent = 'Add Schedule';

    // Populate selects with data
    populateRelaySelect('scheduleRelayTarget');

    // Show relay action by default
    updateScheduleActionContainers();

    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('scheduleModal'));
    modal.show();
}

// Show edit schedule modal
function showEditScheduleModal(scheduleId) {
    const schedule = schedules.find(s => s.id === scheduleId);
    if (!schedule) return;

    // Set form title
    document.getElementById('scheduleModalTitle').textContent = 'Edit Schedule';
    document.getElementById('scheduleId').value = scheduleId;

    // Fill in basic fields
    document.getElementById('scheduleName').value = schedule.name;
    document.getElementById('scheduleTime').value = schedule.time;
    document.getElementById('scheduleEnabled').checked = schedule.enabled;

    // Set days
    document.querySelectorAll('.day-checkbox').forEach(checkbox => {
        checkbox.checked = schedule.days.includes(checkbox.value);
    });

    // Populate selects with data
    populateRelaySelect('scheduleRelayTarget');

    // Set action type and target
    document.getElementById('scheduleActionType').value = schedule.action;
    updateScheduleActionContainers();

    if (schedule.action === 'relay') {
        document.getElementById('scheduleRelayTarget').value = schedule.targetId;
        document.getElementById('scheduleRelayState').value = schedule.targetState;
    } else if (schedule.action === 'scene') {
        document.getElementById('scheduleSceneTarget').value = schedule.targetId;
    } else if (schedule.action === 'notification') {
        document.getElementById('scheduleNotificationMessage').value = schedule.message;
    }

    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('scheduleModal'));
    modal.show();
}

// Update schedule action containers based on selected action type
function updateScheduleActionContainers() {
    const actionType = document.getElementById('scheduleActionType').value;

    document.getElementById('scheduleRelayActionContainer').style.display = 'none';
    document.getElementById('scheduleSceneActionContainer').style.display = 'none';
    document.getElementById('scheduleNotificationActionContainer').style.display = 'none';

    if (actionType === 'relay') {
        document.getElementById('scheduleRelayActionContainer').style.display = 'block';
    } else if (actionType === 'scene') {
        document.getElementById('scheduleSceneActionContainer').style.display = 'block';
    } else if (actionType === 'notification') {
        document.getElementById('scheduleNotificationActionContainer').style.display = 'block';
    }
}

// Add event listener for schedule action type change
document.getElementById('scheduleActionType').addEventListener('change', updateScheduleActionContainers);

// Collect schedule data from the form
function collectScheduleData() {
    const scheduleId = document.getElementById('scheduleId').value;
    const scheduleName = document.getElementById('scheduleName').value;
    const scheduleTime = document.getElementById('scheduleTime').value;
    const enabled = document.getElementById('scheduleEnabled').checked;

    // Collect selected days
    const days = [];
    document.querySelectorAll('.day-checkbox:checked').forEach(checkbox => {
        days.push(checkbox.value);
    });

    // Action settings
    const actionType = document.getElementById('scheduleActionType').value;
    let targetId, targetState, message;

    if (actionType === 'relay') {
        targetId = parseInt(document.getElementById('scheduleRelayTarget').value);
        targetState = document.getElementById('scheduleRelayState').value;
    } else if (actionType === 'scene') {
        targetId = parseInt(document.getElementById('scheduleSceneTarget').value);
    } else if (actionType === 'notification') {
        message = document.getElementById('scheduleNotificationMessage').value;
    }

    // Build the schedule object
    const schedule = {
        name: scheduleName,
        time: scheduleTime,
        days: days,
        enabled: enabled,
        action: actionType
    };

    // Only add ID if editing an existing schedule
    if (scheduleId) {
        schedule.id = parseInt(scheduleId);
    }

    // Add action details based on type
    if (actionType === 'relay') {
        schedule.targetId = targetId;
        schedule.targetState = targetState;
    } else if (actionType === 'scene') {
        schedule.targetId = targetId;
    } else if (actionType === 'notification') {
        schedule.message = message;
    }

    return schedule;
}

// Save schedule
function saveSchedule() {
    // Validate form
    const form = document.getElementById('scheduleForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    // Collect data from form
    const schedule = collectScheduleData();

    // Find where to insert/update the schedule
    let updatedSchedules;

    if (schedule.id !== undefined) {
        // Updating existing schedule
        updatedSchedules = schedules.map(s => s.id === schedule.id ? schedule : s);
    } else {
        // Adding new schedule
        // Assign next available ID
        const maxId = schedules.length > 0 ?
            Math.max(...schedules.map(s => s.id)) : -1;
        schedule.id = maxId + 1;

        updatedSchedules = [...schedules, schedule];
    }

    // Send to the server
    fetch(`${apiBase}/schedules`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ schedules: updatedSchedules })
    })
        .then(response => response.json())
        .then(data => {
            // Close the modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('scheduleModal'));
            modal.hide();

            // Reload schedules
            loadSchedules();
        })
        .catch(error => console.error('Error saving schedule:', error));
}

// ======== UI Update Functions ========

// Update relay UI
function updateRelayUI() {
    const container = document.getElementById('relayContainer');

    // Clear container
    container.innerHTML = '';

    // Create relay cards
    relays.forEach(relay => {
        const relayCard = document.createElement('div');
        relayCard.className = 'col-md-6 mb-3';
        relayCard.innerHTML = `
            <div class="card relay-card ${relay.state ? 'border-success' : 'border-danger'}">
                <div class="card-body">
                    <div class="form-check form-switch">
                        <input class="form-check-input relay-switch" type="checkbox" id="relay${relay.id}" ${relay.state ? 'checked' : ''} data-relay-id="${relay.id}">
                        <label class="form-check-label" for="relay${relay.id}">${relay.name}</label>
                    </div>
                </div>
            </div>
        `;

        container.appendChild(relayCard);

        // Add event listener for the switch
        relayCard.querySelector('.relay-switch').addEventListener('change', function () {
            toggleRelay(relay.id);
        });
    });
}

// Update input UI
function updateInputUI() {
    const container = document.getElementById('inputContainer');

    // Clear container
    container.innerHTML = '';

    // Create input rows
    inputs.forEach(input => {
        const inputRow = document.createElement('div');
        inputRow.className = 'mb-3';
        inputRow.innerHTML = `
            <div class="d-flex align-items-center">
                <span class="input-indicator ${input.state ? 'active' : ''}"></span>
                <span>${input.name}: ${input.state ? 'HIGH' : 'LOW'}</span>
            </div>
        `;

        container.appendChild(inputRow);
    });
}

// Update analog UI
function updateAnalogUI() {
    const container = document.getElementById('analogContainer');

    // Clear container
    container.innerHTML = '';

    // Create analog gauges
    analogInputs.forEach(analog => {
        const analogCol = document.createElement('div');
        analogCol.className = 'col-md-6 mb-3';

        // Calculate percentage for display
        const percentage = (analog.value / 4095) * 100;

        analogCol.innerHTML = `
            <div class="card">
                <div class="card-body">
                    <h6>${analog.name}</h6>
                    <div class="progress">
                        <div class="progress-bar bg-info" role="progressbar" style="width: ${percentage}%"
                             aria-valuenow="${analog.value}" aria-valuemin="0" aria-valuemax="4095"></div>
                    </div>
                    <div class="text-center mt-2">
                        <span>${analog.value} ${analog.unit || ''}</span>
                    </div>
                </div>
            </div>
        `;

        container.appendChild(analogCol);
    });
}

// Update automation rules UI
function updateAutomationUI() {
    const tableBody = document.querySelector('#automationRulesTable tbody');

    // Clear table
    tableBody.innerHTML = '';

    // Add rows for each rule
    automationRules.forEach(rule => {
        const row = document.createElement('tr');

        // Format condition text
        let conditionText = '';
        if (rule.conditions && rule.conditions.length > 0) {
            rule.conditions.forEach((condition, index) => {
                if (index > 0) {
                    conditionText += ` ${rule.logicOperator} `;
                }

                if (condition.type === 'digital') {
                    // Find input name
                    const input = inputs.find(i => i.id === condition.sourceId) || { name: `Input ${condition.sourceId + 1}` };
                    conditionText += `${input.name} is ${condition.condition}`;
                } else if (condition.type === 'analog') {
                    // Find analog input name
                    const analog = analogInputs.find(a => a.id === condition.sourceId) || { name: `Analog ${condition.sourceId + 1}` };

                    if (condition.condition === 'gt') {
                        conditionText += `${analog.name} > ${condition.threshold1}`;
                    } else if (condition.condition === 'lt') {
                        conditionText += `${analog.name} < ${condition.threshold1}`;
                    } else if (condition.condition === 'eq') {
                        conditionText += `${analog.name} = ${condition.threshold1}`;
                    } else if (condition.condition === 'between') {
                        conditionText += `${analog.name} between ${condition.threshold1} and ${condition.threshold2}`;
                    }
                }
            });
        }

        // Format timer text
        let timerText = rule.useTimer ?
            `${rule.timerType === 'ondelay' ? 'ON Delay' : 'OFF Delay'} ${rule.timerDuration}ms` :
            'None';

        // Format action text
        let actionText = '';
        if (rule.action === 'relay') {
            // Find relay name
            const relay = relays.find(r => r.id === rule.targetId) || { name: `Relay ${rule.targetId + 1}` };
            actionText = `${relay.name} ${rule.targetState.toUpperCase()}`;
        } else if (rule.action === 'scene') {
            actionText = `Scene ${rule.targetId + 1}`;
        } else if (rule.action === 'notification') {
            actionText = `Notification: "${rule.message.substring(0, 20)}${rule.message.length > 20 ? '...' : ''}"`;
        }

        row.innerHTML = `
            <td>${rule.name}</td>
            <td>${conditionText}</td>
            <td>${rule.conditions && rule.conditions.length > 1 ? rule.logicOperator : '-'}</td>
            <td>${timerText}</td>
            <td>${actionText}</td>
            <td>
                <div class="form-check form-switch">
                    <input class="form-check-input" type="checkbox" ${rule.enabled ? 'checked' : ''} data-rule-id="${rule.id}">
                </div>
            </td>
            <td>
                <button class="btn btn-sm btn-primary edit-rule" data-rule-id="${rule.id}">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="btn btn-sm btn-danger delete-rule" data-rule-id="${rule.id}">
                    <i class="bi bi-trash"></i>
                </button>
            </td>
        `;

        tableBody.appendChild(row);
    });

    // Add event listeners
    document.querySelectorAll('#automationRulesTable .form-check-input').forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            const ruleId = parseInt(this.getAttribute('data-rule-id'));
            toggleAutomationRule(ruleId);
        });
    });

    document.querySelectorAll('#automationRulesTable .edit-rule').forEach(button => {
        button.addEventListener('click', function () {
            const ruleId = parseInt(this.getAttribute('data-rule-id'));
            showEditAutomationModal(ruleId);
        });
    });

    document.querySelectorAll('#automationRulesTable .delete-rule').forEach(button => {
        button.addEventListener('click', function () {
            const ruleId = parseInt(this.getAttribute('data-rule-id'));
            deleteAutomationRule(ruleId);
        });
    });
}

// Update schedules UI
function updateSchedulesUI() {
    const tableBody = document.querySelector('#schedulesTable tbody');

    // Clear table
    tableBody.innerHTML = '';

    // Add rows for each schedule
    schedules.forEach(schedule => {
        const row = document.createElement('tr');

        // Format days text
        const daysText = schedule.days.join(', ');

        // Format action text
        let actionText = '';
        if (schedule.action === 'relay') {
            // Find relay name
            const relay = relays.find(r => r.id === schedule.targetId) || { name: `Relay ${schedule.targetId + 1}` };
            actionText = `${relay.name} ${schedule.targetState.toUpperCase()}`;
        } else if (schedule.action === 'scene') {
            actionText = `Scene ${schedule.targetId + 1}`;
        } else if (schedule.action === 'notification') {
            actionText = `Notification: "${schedule.message.substring(0, 20)}${schedule.message.length > 20 ? '...' : ''}"`;
        }

        row.innerHTML = `
            <td>${schedule.name}</td>
            <td>${schedule.time}</td>
            <td>${daysText}</td>
            <td>${actionText}</td>
            <td>
                <div class="form-check form-switch">
                    <input class="form-check-input" type="checkbox" ${schedule.enabled ? 'checked' : ''} data-schedule-id="${schedule.id}">
                </div>
            </td>
            <td>
                <button class="btn btn-sm btn-primary edit-schedule" data-schedule-id="${schedule.id}">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="btn btn-sm btn-danger delete-schedule" data-schedule-id="${schedule.id}">
                    <i class="bi bi-trash"></i>
                </button>
            </td>
        `;

        tableBody.appendChild(row);
    });

    // Add event listeners
    document.querySelectorAll('#schedulesTable .form-check-input').forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            const scheduleId = parseInt(this.getAttribute('data-schedule-id'));
            toggleSchedule(scheduleId);
        });
    });

    document.querySelectorAll('#schedulesTable .edit-schedule').forEach(button => {
        button.addEventListener('click', function () {
            const scheduleId = parseInt(this.getAttribute('data-schedule-id'));
            showEditScheduleModal(scheduleId);
        });
    });

    document.querySelectorAll('#schedulesTable .delete-schedule').forEach(button => {
        button.addEventListener('click', function () {
            const scheduleId = parseInt(this.getAttribute('data-schedule-id'));
            deleteSchedule(scheduleId);
        });
    });
}

// Update I/O settings UI
function updateIOSettingsUI() {
    // Update relay settings
    const relayContainer = document.getElementById('relaySettingsContainer');
    relayContainer.innerHTML = '';

    relays.forEach(relay => {
        const relaySettings = document.createElement('div');
        relaySettings.className = 'relay-setting mb-3 p-3 border rounded';
        relaySettings.setAttribute('data-relay-id', relay.id);

        relaySettings.innerHTML = `
            <div class="row">
                <div class="col-md-6">
                    <div class="mb-2">
                        <label class="form-label">Relay ${relay.id + 1} Name</label>
                        <input type="text" class="form-control relay-name" value="${relay.name}">
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="form-check mt-4">
                        <input class="form-check-input relay-invert" type="checkbox" ${relay.invertLogic ? 'checked' : ''}>
                        <label class="form-check-label">Invert Logic</label>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="form-check mt-4">
                        <input class="form-check-input relay-remember" type="checkbox" ${relay.rememberState ? 'checked' : ''}>
                        <label class="form-check-label">Remember State</label>
                    </div>
                </div>
            </div>
        `;

        relayContainer.appendChild(relaySettings);
    });

    // Update input settings
    const inputContainer = document.getElementById('inputSettingsContainer');
    inputContainer.innerHTML = '';

    inputs.forEach(input => {
        const inputSettings = document.createElement('div');
        inputSettings.className = 'input-setting mb-3 p-3 border rounded';
        inputSettings.setAttribute('data-input-id', input.id);

        inputSettings.innerHTML = `
            <div class="row">
                <div class="col-md-6">
                    <div class="mb-2">
                        <label class="form-label">Input ${input.id + 1} Name</label>
                        <input type="text" class="form-control input-name" value="${input.name}">
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="form-check mt-4">
                        <input class="form-check-input input-invert" type="checkbox" ${input.invertLogic ? 'checked' : ''}>
                        <label class="form-check-label">Invert Logic</label>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="mb-2">
                        <label class="form-label">Mode</label>
                        <select class="form-select input-mode">
                            <option value="normal" ${input.mode === 'normal' ? 'selected' : ''}>Normal</option>
                            <option value="toggle" ${input.mode === 'toggle' ? 'selected' : ''}>Toggle</option>
                            <option value="push" ${input.mode === 'push' ? 'selected' : ''}>Push Button</option>
                        </select>
                    </div>
                </div>
            </div>
        `;

        inputContainer.appendChild(inputSettings);
    });

    // Update analog input settings
    const analogContainer = document.getElementById('analogSettingsContainer');
    analogContainer.innerHTML = '';

    analogInputs.forEach(analog => {
        const analogSettings = document.createElement('div');
        analogSettings.className = 'analog-setting mb-3 p-3 border rounded';
        analogSettings.setAttribute('data-analog-id', analog.id);

        analogSettings.innerHTML = `
            <div class="row">
                <div class="col-md-4">
                    <div class="mb-2">
                        <label class="form-label">Analog ${analog.id + 1} Name</label>
                        <input type="text" class="form-control analog-name" value="${analog.name}">
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="mb-2">
                        <label class="form-label">Mode</label>
                        <select class="form-select analog-mode">
                            <option value="raw" ${analog.mode === 'raw' ? 'selected' : ''}>Raw Value</option>
                            <option value="voltage" ${analog.mode === 'voltage' ? 'selected' : ''}>Voltage</option>
                            <option value="percent" ${analog.mode === 'percent' ? 'selected' : ''}>Percentage</option>
                            <option value="custom" ${analog.mode === 'custom' ? 'selected' : ''}>Custom</option>
                        </select>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="mb-2">
                        <label class="form-label">Unit</label>
                        <input type="text" class="form-control analog-unit" value="${analog.unit || ''}">
                    </div>
                </div>
            </div>
        `;

        analogContainer.appendChild(analogSettings);
    });
}

// Update settings UI
function updateSettingsUI(data) {
    // Network settings
    document.getElementById('useEthernet').checked = data.network.useEthernet;
    document.getElementById('wifiSsid').value = data.network.wifi.ssid;
    document.getElementById('useDhcp').checked = data.network.wifi.dhcp;
    document.getElementById('staticIp').value = data.network.wifi.ip;
    document.getElementById('gateway').value = data.network.wifi.gateway;
    document.getElementById('subnet').value = data.network.wifi.subnet;
    document.getElementById('dns').value = data.network.wifi.dns;

    // Show/hide wifi settings based on Ethernet enabled
    document.getElementById('wifiSettings').style.display = data.network.useEthernet ? 'none' : 'block';

    // Show/hide static IP settings based on DHCP
    document.getElementById('staticIpSettings').style.display = data.network.wifi.dhcp ? 'none' : 'block';

    // Device settings
    document.getElementById('deviceName').value = data.device.name;

    // MQTT settings
    document.getElementById('mqttEnabled').checked = data.mqtt.enabled;
    document.getElementById('mqttServer').value = data.mqtt.server;
    document.getElementById('mqttPort').value = data.mqtt.port;
    document.getElementById('mqttUser').value = data.mqtt.username;

    // Show/hide MQTT settings based on enabled
    document.getElementById('mqttSettings').style.display = data.mqtt.enabled ? 'block' : 'none';

    // Alexa settings
    document.getElementById('alexaEnabled').checked = data.alexa.enabled;
    document.getElementById('alexaDeviceName').value = data.alexa.deviceName;

    // Show/hide Alexa settings based on enabled
    document.getElementById('alexaSettings').style.display = data.alexa.enabled ? 'block' : 'none';

    // USB settings
    document.getElementById('usbEnabled').checked = data.usb.enabled;
    document.getElementById('baudRate').value = data.usb.baudRate;

    // Show/hide USB settings based on enabled
    document.getElementById('usbSettings').style.display = data.usb.enabled ? 'block' : 'none';
}

// ======== Utility Functions ========

// Populate relay select in forms
function populateRelaySelect(selectId) {
    const select = document.getElementById(selectId);
    select.innerHTML = '';

    relays.forEach(relay => {
        const option = document.createElement('option');
        option.value = relay.id;
        option.textContent = relay.name;
        select.appendChild(option);
    });
}

// Populate input selects in condition forms
function populateInputSelect() {
    const selects = document.querySelectorAll('.digital-input');

    selects.forEach(select => {
        // Save current value
        const currentValue = select.value;

        // Clear select
        select.innerHTML = '';

        // Add options
        inputs.forEach(input => {
            const option = document.createElement('option');
            option.value = input.id;
            option.textContent = input.name;
            select.appendChild(option);
        });

        // Restore value if possible
        if (currentValue) {
            select.value = currentValue;
        }
    });
}

// Populate analog input selects in condition forms
function populateAnalogSelect() {
    const selects = document.querySelectorAll('.analog-input');

    selects.forEach(select => {
        // Save current value
        const currentValue = select.value;

        // Clear select
        select.innerHTML = '';

        // Add options
        analogInputs.forEach(analog => {
            const option = document.createElement('option');
            option.value = analog.id;
            option.textContent = analog.name;
            select.appendChild(option);
        });

        // Restore value if possible
        if (currentValue) {
            select.value = currentValue;
        }
    });
}

// Format uptime
function formatUptime(seconds) {
    if (!seconds) return 'Unknown';

    const days = Math.floor(seconds / 86400);
    const hours = Math.floor((seconds % 86400) / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;

    let result = '';
    if (days > 0) result += `${days}d `;
    if (hours > 0 || days > 0) result += `${hours}h `;
    if (minutes > 0 || hours > 0 || days > 0) result += `${minutes}m `;
    result += `${secs}s`;

    return result;
}
