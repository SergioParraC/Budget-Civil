import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';

import { AuthService } from '../../../core/auth/auth.service';
import { resolveApiError } from '../../../core/http/api-error';

@Component({
  selector: 'app-recuperar-contrasena',
  imports: [ReactiveFormsModule, RouterLink, ButtonModule, CardModule, InputTextModule, MessageModule],
  templateUrl: './recuperar-contrasena.html',
  styleUrl: './recuperar-contrasena.scss'
})
export class RecuperarContrasena {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  readonly sending = signal(false);
  readonly sent = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Escribe un correo electrónico válido.');
      return;
    }

    this.sending.set(true);
    this.errorMessage.set(null);
    this.authService.requestPasswordRecovery(this.form.getRawValue().email).subscribe({
      next: () => {
        this.sending.set(false);
        this.sent.set(true);
      },
      error: (error: unknown) => {
        this.sending.set(false);
        this.errorMessage.set(resolveApiError(error).message);
      }
    });
  }
}
