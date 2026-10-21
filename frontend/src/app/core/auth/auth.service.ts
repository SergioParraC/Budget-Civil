import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthenticatedUser, LoginRequest, LoginResponse } from '../models/auth.model';

const STORAGE_KEY = 'budget-civil-session';

interface StoredSession {
  accessToken: string;
  user: AuthenticatedUser;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<StoredSession | null>(this.readStoredSession());

  readonly currentUser = computed(() => this.session()?.user ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly accessToken = computed(() => this.session()?.accessToken ?? null);

  login(request: LoginRequest) {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap((response) => this.setSession(response))
    );
  }

  logout() {
    this.session.set(null);
    localStorage.removeItem(STORAGE_KEY);
    this.router.navigate(['/login']);
  }

  private setSession(response: LoginResponse) {
    const stored: StoredSession = {
      accessToken: response.access_token,
      user: response.user,
      expiresAt: response.expires_at
    };
    this.session.set(stored);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
  }

  private readStoredSession(): StoredSession | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as StoredSession;
    } catch {
      return null;
    }
  }
}
