import { Component, inject, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountService } from '../_services/account.service';
import { ToastrService } from 'ngx-toastr';
import { QRCodeComponent } from 'angularx-qrcode'; 

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, QRCodeComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private accountService= inject(AccountService);
  private toastr = inject(ToastrService)
  cancelRegister = output<boolean>();
  model: any = {}
  twoFactorQrCodeUrl: string = '';

  register() {
    this.accountService.register(this.model).subscribe({
      next: (response: any) => {
        this.twoFactorQrCodeUrl = response.twoFactorQrCodeUrl;
        this.toastr.success('Rejestracja przebiegla pomyslnie, prosze zeskanowac kod qr');
      },
      error: (error) => {
        this.toastr.error(error.error);
      }
    });
  }
  cancel(){
    console.log("close")
    this.cancelRegister.emit(false);
  }
}
