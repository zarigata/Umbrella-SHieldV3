/**
 * ZariVirusKiller Admin Dashboard
 * Main TypeScript file for the admin dashboard functionality
 */

// Types
interface License {
  id: number;
  key: string;
  created_at: string;
  expires_at: string;
  device_id: string | null;
}

interface Signature {
  id: number;
  name: string;
  signature_type: 'hash' | 'pattern';
  hash_value?: string;
  signature_id?: string;
  pattern_data?: string;
  severity: 'low' | 'medium' | 'high';
  description?: string;
  created_at: string;
}

interface DefinitionUpdate {
  id: number;
  version: string;
  path: string;
  uploaded_at: string;
  signature_count: number;
  update_type: 'hash' | 'pattern';
}

interface User {
  id: number;
  username: string;
  is_admin: boolean;
  created_at: string;
}

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
}

// API Service
class ApiService {
  private baseUrl: string = '/api';
  private authUrl: string = '/auth';
  private token: string | null = null;

  constructor() {
    // Check if token exists in localStorage
    this.token = localStorage.getItem('auth_token');
  }

  private async request<T>(url: string, method: string = 'GET', data?: any): Promise<ApiResponse<T>> {
    const headers: HeadersInit = {
      'Content-Type': 'application/json'
    };

    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    try {
      const response = await fetch(url, {
        method,
        headers,
        body: data ? JSON.stringify(data) : undefined
      });

      if (response.status === 401) {
        // Token expired or invalid
        this.token = null;
        localStorage.removeItem('auth_token');
        window.location.reload();
        return { success: false, error: 'Authentication required' };
      }

      const result = await response.json();
      return { success: response.ok, data: result, error: !response.ok ? result.error : undefined };
    } catch (error) {
      console.error('API request failed:', error);
      return { success: false, error: 'Network error' };
    }
  }

  async login(username: string, password: string): Promise<boolean> {
    const response = await this.request<{ access_token: string }>(`${this.authUrl}/login`, 'POST', { username, password });
    
    if (response.success && response.data?.access_token) {
      this.token = response.data.access_token;
      localStorage.setItem('auth_token', this.token);
      return true;
    }
    
    return false;
  }

  async getLicenses(): Promise<License[]> {
    const response = await this.request<License[]>(`${this.baseUrl}/license/all`);
    return response.success && response.data ? response.data : [];
  }

  async getSignatures(): Promise<Signature[]> {
    const response = await this.request<Signature[]>(`${this.baseUrl}/signatures`);
    return response.success && response.data ? response.data : [];
  }

  async getDefinitions(): Promise<DefinitionUpdate[]> {
    const response = await this.request<DefinitionUpdate[]>(`${this.baseUrl}/definitions`);
    return response.success && response.data ? response.data : [];
  }

  async createLicense(expiryDays: number): Promise<License | null> {
    const response = await this.request<License>(`${this.baseUrl}/license/create`, 'POST', { expiry_days: expiryDays });
    return response.success && response.data ? response.data : null;
  }

  async uploadDefinition(file: File, version: string, updateType: 'hash' | 'pattern'): Promise<boolean> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('version', version);
    formData.append('update_type', updateType);

    const headers: HeadersInit = {};
    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    try {
      const response = await fetch(`${this.baseUrl}/definitions/upload`, {
        method: 'POST',
        headers,
        body: formData
      });

      return response.ok;
    } catch (error) {
      console.error('Upload failed:', error);
      return false;
    }
  }

  async logout(): Promise<void> {
    localStorage.removeItem('auth_token');
    this.token = null;
  }
}

// Dashboard Controller
class DashboardController {
  private api: ApiService;
  private currentUser: User | null = null;

  constructor() {
    this.api = new ApiService();
    this.initEventListeners();
    this.checkAuthentication();
  }

  private async checkAuthentication(): Promise<void> {
    const token = localStorage.getItem('auth_token');
    
    if (token) {
      // Show dashboard
      document.getElementById('login-form')!.classList.add('hidden');
      document.getElementById('dashboard-content')!.classList.remove('hidden');
      document.getElementById('user-info')!.classList.remove('hidden');
      
      // Load dashboard data
      await this.loadDashboardData();
    } else {
      // Show login form
      document.getElementById('login-form')!.classList.remove('hidden');
      document.getElementById('dashboard-content')!.classList.add('hidden');
      document.getElementById('user-info')!.classList.add('hidden');
    }
  }

  private initEventListeners(): void {
    // Login button
    document.getElementById('login-btn')!.addEventListener('click', async () => {
      const username = (document.getElementById('username-input') as HTMLInputElement).value;
      const password = (document.getElementById('password-input') as HTMLInputElement).value;
      
      if (!username || !password) {
        this.showLoginError('Please enter both username and password');
        return;
      }
      
      const success = await this.api.login(username, password);
      
      if (success) {
        this.checkAuthentication();
      } else {
        this.showLoginError('Invalid username or password');
      }
    });

    // Logout button
    document.getElementById('logout-btn')!.addEventListener('click', async () => {
      await this.api.logout();
      this.checkAuthentication();
    });

    // Tab navigation
    const tabButtons = document.querySelectorAll('[data-tab]');
    tabButtons.forEach(button => {
      button.addEventListener('click', () => {
        const tabId = button.getAttribute('data-tab');
        this.switchTab(tabId!);
      });
    });

    // Create license form
    document.getElementById('create-license-btn')?.addEventListener('click', async () => {
      const expiryDaysInput = document.getElementById('expiry-days') as HTMLInputElement;
      const expiryDays = parseInt(expiryDaysInput.value, 10);
      
      if (isNaN(expiryDays) || expiryDays <= 0) {
        alert('Please enter a valid number of days');
        return;
      }
      
      const license = await this.api.createLicense(expiryDays);
      
      if (license) {
        alert(`License created successfully: ${license.key}`);
        this.loadLicenses();
      } else {
        alert('Failed to create license');
      }
    });

    // Upload definition form
    document.getElementById('upload-definition-btn')?.addEventListener('click', async () => {
      const fileInput = document.getElementById('definition-file') as HTMLInputElement;
      const versionInput = document.getElementById('definition-version-input') as HTMLInputElement;
      const typeSelect = document.getElementById('definition-type') as HTMLSelectElement;
      
      if (!fileInput.files || fileInput.files.length === 0) {
        alert('Please select a file');
        return;
      }
      
      const file = fileInput.files[0];
      const version = versionInput.value;
      const updateType = typeSelect.value as 'hash' | 'pattern';
      
      if (!version) {
        alert('Please enter a version');
        return;
      }
      
      const success = await this.api.uploadDefinition(file, version, updateType);
      
      if (success) {
        alert('Definition uploaded successfully');
        this.loadDefinitions();
      } else {
        alert('Failed to upload definition');
      }
    });
  }

  private showLoginError(message: string): void {
    const errorElement = document.getElementById('login-error')!;
    errorElement.textContent = message;
    errorElement.classList.remove('hidden');
  }

  private switchTab(tabId: string): void {
    // Hide all tab contents
    document.querySelectorAll('.tab-content').forEach(tab => {
      tab.classList.add('hidden');
    });
    
    // Deactivate all tab buttons
    document.querySelectorAll('[data-tab]').forEach(button => {
      button.classList.remove('bg-blue-500', 'text-white');
      button.classList.add('bg-gray-200', 'text-gray-700');
    });
    
    // Show selected tab content
    document.getElementById(`${tabId}-tab`)!.classList.remove('hidden');
    
    // Activate selected tab button
    document.querySelector(`[data-tab="${tabId}"]`)!.classList.remove('bg-gray-200', 'text-gray-700');
    document.querySelector(`[data-tab="${tabId}"]`)!.classList.add('bg-blue-500', 'text-white');
  }

  private async loadDashboardData(): Promise<void> {
    await Promise.all([
      this.loadLicenses(),
      this.loadSignatures(),
      this.loadDefinitions()
    ]);
  }

  private async loadLicenses(): Promise<void> {
    const licenses = await this.api.getLicenses();
    
    // Update statistics
    document.getElementById('total-licenses')!.textContent = licenses.length.toString();
    
    const now = new Date();
    const activeLicenses = licenses.filter(license => new Date(license.expires_at) > now);
    document.getElementById('active-licenses')!.textContent = activeLicenses.length.toString();
    
    const usedLicenses = licenses.filter(license => license.device_id !== null);
    document.getElementById('used-licenses')!.textContent = usedLicenses.length.toString();
    
    // Update license table
    const tableBody = document.getElementById('licenses-table-body')!;
    tableBody.innerHTML = '';
    
    licenses.forEach(license => {
      const row = document.createElement('tr');
      
      const expiryDate = new Date(license.expires_at);
      const isExpired = expiryDate < now;
      
      row.innerHTML = `
        <td class="border px-4 py-2">${license.key}</td>
        <td class="border px-4 py-2">${new Date(license.created_at).toLocaleDateString()}</td>
        <td class="border px-4 py-2 ${isExpired ? 'text-red-500' : ''}">${expiryDate.toLocaleDateString()}</td>
        <td class="border px-4 py-2">${license.device_id || 'Not activated'}</td>
      `;
      
      tableBody.appendChild(row);
    });
  }

  private async loadSignatures(): Promise<void> {
    const signatures = await this.api.getSignatures();
    
    // Update statistics
    document.getElementById('total-signatures')!.textContent = signatures.length.toString();
    
    const hashSignatures = signatures.filter(sig => sig.signature_type === 'hash');
    document.getElementById('hash-signatures')!.textContent = hashSignatures.length.toString();
    
    const patternSignatures = signatures.filter(sig => sig.signature_type === 'pattern');
    document.getElementById('pattern-signatures')!.textContent = patternSignatures.length.toString();
    
    // Update signatures table
    const tableBody = document.getElementById('signatures-table-body')!;
    tableBody.innerHTML = '';
    
    signatures.forEach(signature => {
      const row = document.createElement('tr');
      
      let severityClass = '';
      if (signature.severity === 'high') severityClass = 'text-red-500';
      else if (signature.severity === 'medium') severityClass = 'text-yellow-500';
      else severityClass = 'text-green-500';
      
      row.innerHTML = `
        <td class="border px-4 py-2">${signature.name}</td>
        <td class="border px-4 py-2">${signature.signature_type}</td>
        <td class="border px-4 py-2 ${severityClass}">${signature.severity}</td>
        <td class="border px-4 py-2">${new Date(signature.created_at).toLocaleDateString()}</td>
      `;
      
      tableBody.appendChild(row);
    });
  }

  private async loadDefinitions(): Promise<void> {
    const definitions = await this.api.getDefinitions();
    
    if (definitions.length > 0) {
      // Sort by date (newest first)
      definitions.sort((a, b) => new Date(b.uploaded_at).getTime() - new Date(a.uploaded_at).getTime());
      
      const latestDefinition = definitions[0];
      
      // Update statistics
      document.getElementById('latest-update')!.textContent = new Date(latestDefinition.uploaded_at).toLocaleDateString();
      document.getElementById('definition-version')!.textContent = latestDefinition.version;
      document.getElementById('definition-count')!.textContent = latestDefinition.signature_count.toString();
    }
    
    // Update definitions table
    const tableBody = document.getElementById('definitions-table-body')!;
    tableBody.innerHTML = '';
    
    definitions.forEach(definition => {
      const row = document.createElement('tr');
      
      row.innerHTML = `
        <td class="border px-4 py-2">${definition.version}</td>
        <td class="border px-4 py-2">${definition.update_type}</td>
        <td class="border px-4 py-2">${definition.signature_count}</td>
        <td class="border px-4 py-2">${new Date(definition.uploaded_at).toLocaleDateString()}</td>
      `;
      
      tableBody.appendChild(row);
    });
  }
}

// Initialize the dashboard when the DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
  new DashboardController();
});