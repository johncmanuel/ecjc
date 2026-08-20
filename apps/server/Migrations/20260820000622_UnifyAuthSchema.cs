using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class UnifyAuthSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entries_Users_AuthorId",
                table: "Entries");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupInvites_Users_InviteeId",
                table: "GroupInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupInvites_Users_InviterId",
                table: "GroupInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupUsers_Users_UserId",
                table: "GroupUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Reactions_Users_UserId",
                table: "Reactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "user");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "user",
                newName: "updatedAt");

            migrationBuilder.RenameColumn(
                name: "StripeCustomerId",
                table: "user",
                newName: "stripeCustomerId");

            migrationBuilder.RenameColumn(
                name: "PenaltyAmount",
                table: "user",
                newName: "penaltyAmount");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "user",
                newName: "lastName");

            migrationBuilder.RenameColumn(
                name: "IsPenaltyEnabled",
                table: "user",
                newName: "isPenaltyEnabled");

            migrationBuilder.RenameColumn(
                name: "Image",
                table: "user",
                newName: "image");

            migrationBuilder.RenameColumn(
                name: "FriendCode",
                table: "user",
                newName: "friendCode");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "user",
                newName: "firstName");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "user",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "user",
                newName: "createdAt");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "user",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Users_FriendCode",
                table: "user",
                newName: "IX_user_friendCode");

            migrationBuilder.AddColumn<bool>(
                name: "emailVerified",
                table: "user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "user",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user",
                table: "user",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Entries_user_AuthorId",
                table: "Entries",
                column: "AuthorId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupInvites_user_InviteeId",
                table: "GroupInvites",
                column: "InviteeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupInvites_user_InviterId",
                table: "GroupInvites",
                column: "InviterId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupUsers_user_UserId",
                table: "GroupUsers",
                column: "UserId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reactions_user_UserId",
                table: "Reactions",
                column: "UserId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entries_user_AuthorId",
                table: "Entries");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupInvites_user_InviteeId",
                table: "GroupInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupInvites_user_InviterId",
                table: "GroupInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupUsers_user_UserId",
                table: "GroupUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Reactions_user_UserId",
                table: "Reactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user",
                table: "user");

            migrationBuilder.DropColumn(
                name: "emailVerified",
                table: "user");

            migrationBuilder.DropColumn(
                name: "name",
                table: "user");

            migrationBuilder.RenameTable(
                name: "user",
                newName: "Users");

            migrationBuilder.RenameColumn(
                name: "updatedAt",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "stripeCustomerId",
                table: "Users",
                newName: "StripeCustomerId");

            migrationBuilder.RenameColumn(
                name: "penaltyAmount",
                table: "Users",
                newName: "PenaltyAmount");

            migrationBuilder.RenameColumn(
                name: "lastName",
                table: "Users",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "isPenaltyEnabled",
                table: "Users",
                newName: "IsPenaltyEnabled");

            migrationBuilder.RenameColumn(
                name: "image",
                table: "Users",
                newName: "Image");

            migrationBuilder.RenameColumn(
                name: "friendCode",
                table: "Users",
                newName: "FriendCode");

            migrationBuilder.RenameColumn(
                name: "firstName",
                table: "Users",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "createdAt",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_user_friendCode",
                table: "Users",
                newName: "IX_Users_FriendCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Entries_Users_AuthorId",
                table: "Entries",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupInvites_Users_InviteeId",
                table: "GroupInvites",
                column: "InviteeId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupInvites_Users_InviterId",
                table: "GroupInvites",
                column: "InviterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupUsers_Users_UserId",
                table: "GroupUsers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reactions_Users_UserId",
                table: "Reactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
