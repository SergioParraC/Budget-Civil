import { Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';

/** Estado común de las pantallas del proyecto cuando `GET /projects/{id}` responde 404 o 403. */
@Component({
  selector: 'app-project-unavailable',
  imports: [ButtonModule, MessageModule],
  templateUrl: './project-unavailable.html',
  styleUrl: './project-unavailable.scss'
})
export class ProjectUnavailable {
  readonly retry = output<void>();

  readonly detail = input(
    'El proyecto no existe o no tienes acceso a él. Vuelve al listado y elige un proyecto disponible.'
  );
}
