import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AuthenticatedUser,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse
} from '../models/auth.model';
import { decodeTokenClaims } from './jwt.util';
import { PermissionKey } from './permissions';

const STORAGE_KEY = 'budget-civil-session';
const SESSION_VERSION = 2;

interface StoredSession {
  version: number;
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
  readonly roleCode = computed(() => decodeTokenClaims(this.session()?.accessToken).roleCode);

  private readonly permissions = computed(() => new Set(decodeTokenClaims(this.session()?.accessToken).permissions));

  /** El código de rol y los permisos solo viven en el token, no en `LoginResponse.user`. */
  can(permission: PermissionKey): boolean {
    return this.permissions().has(permission);
  }

  login(request: LoginRequest) {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap((response) => this.setSession(response))
    );
  }

  register(request: RegisterRequest) {
    return this.http.post<RegisterResponse>(`${environment.apiUrl}/auth/register`, request);
  }

  requestPasswordRecovery(email: string) {
    return this.http.post<void>(`${environment.apiUrl}/auth/password-recovery`, { email });
  }

  logout() {
    this.session.set(null);
    localStorage.removeItem(STORAGE_KEY);
    this.router.navigate(['/login']);
  }

  private setSession(response: LoginResponse) {
    const stored: StoredSession = {
      version: SESSION_VERSION,
      accessToken: response.accessToken,
      user: response.user,
      expiresAt: response.expiresAtUtc
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
      const parsed = JSON.parse(raw) as Partial<StoredSession>;
      if (parsed.version !== SESSION_VERSION || typeof parsed.accessToken !== 'string' || !parsed.user) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }

      return parsed as StoredSession;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
  }
}
