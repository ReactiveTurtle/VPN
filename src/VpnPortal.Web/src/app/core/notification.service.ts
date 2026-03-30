import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.open(message, 3500);
  }

  error(message: string): void {
    this.open(message, 4500);
  }

  info(message: string): void {
    this.open(message, 3000);
  }

  private open(message: string, duration: number): void {
    this.snackBar.open(message, 'Закрыть', {
      duration,
      horizontalPosition: 'right',
      verticalPosition: 'top'
    });
  }
}
