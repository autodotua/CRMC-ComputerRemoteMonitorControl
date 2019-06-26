create database CRMC
go

use CRMC

create table User_Login
(
	Id int primary key identity(1,1) not null ,
	Name nvarchar(30) not null,
	Password char(32) not null default('670B14728AD9902AECBA32E22FA4F6BD'),
	Role nvarchar(5) not null check (Role in('用户','管理员'))
)
