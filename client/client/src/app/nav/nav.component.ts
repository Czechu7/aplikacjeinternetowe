import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountService } from '../_services/account.service';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TitleCasePipe } from '@angular/common';
@Component({
  selector: 'app-nav',
  standalone: true,
  imports: [FormsModule, BsDropdownModule,RouterLink, RouterLinkActive, TitleCasePipe],
  templateUrl: './nav.component.html',
  styleUrl: './nav.component.css'
})
export class NavComponent {
  accountService = inject(AccountService)
  private router = inject(Router)
  private toastr = inject(ToastrService)
  twoFactorRequired: boolean = false;

  model: any = {};

  login() {
    this.accountService.login(this.model).subscribe({
      next: (response : any) => {
        console.log(response);
        if (response.twoFactorRequired) {
          this.twoFactorRequired = true;
        } else {
          this.router.navigateByUrl('/members');
        }
      },
      error: (error) => this.toastr.error(error.error)
    });
  }
  
  verifyTwoFactorCode() {
    this.accountService.verifyTwoFactorCode(this.model.username, this.model.twoFactorCode).subscribe({
      next: (response) => {
        this.accountService.currentUser.set({
          username: response.username,
          token: response.token,
          enable2fa: true,
        });
        this.router.navigateByUrl('/members'); 
      },
      error: (error) => this.toastr.error(error.error)
    });
  }
  
  logout(){
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }
}
