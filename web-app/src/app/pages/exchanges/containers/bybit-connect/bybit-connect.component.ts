import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-bybit-connect',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './bybit-connect.component.html',
})
export class BybitConnectComponent {
  apiKey = '';
  apiSecret = '';
  webhookSecret = '';
  showSecrets = false;
  state: 'form' | 'saved' | 'verified' | 'discovered' = 'form';
  errorMessage = '';

  saveCredentials(): void {
    if (!this.apiKey.trim() || !this.apiSecret.trim()) {
      this.errorMessage = 'Enter both an API key and API secret to continue.';
      return;
    }

    this.errorMessage = '';
    this.state = 'saved';
  }

  testConnection(): void {
    this.state = 'verified';
  }

  discoverAccounts(): void {
    this.state = 'discovered';
  }
}
