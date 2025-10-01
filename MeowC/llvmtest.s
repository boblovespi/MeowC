	.build_version macos, 14, 0
	.section	__TEXT,__text,regular,pure_instructions
	.globl	_foo
	.p2align	2
_foo:
	.cfi_startproc
	mul	w9, w0, w0
	add	w8, w0, #30
	add	w0, w8, w9
	ret
	.cfi_endproc

	.globl	_bar
	.p2align	2
_bar:
	.cfi_startproc
	sub	sp, sp, #32
	stp	x29, x30, [sp, #16]
	.cfi_def_cfa_offset 32
	.cfi_offset w30, -8
	.cfi_offset w29, -16
	str	w0, [sp, #8]
	bl	_foo
	mov	w8, w0
	ldr	w0, [sp, #8]
	str	w8, [sp, #12]
	add	w0, w0, #1
	bl	_foo
	ldr	w8, [sp, #12]
	add	w0, w0, w8
	ldp	x29, x30, [sp, #16]
	add	sp, sp, #32
	ret
	.cfi_endproc

	.globl	_fac
	.p2align	2
_fac:
	.cfi_startproc
	sub	sp, sp, #16
	.cfi_def_cfa_offset 16
	mov	w8, #1
	str	w8, [sp, #8]
	str	w0, [sp, #12]
	b	LBB2_1
LBB2_1:
	ldr	w9, [sp, #8]
	ldr	w8, [sp, #12]
	str	w8, [sp]
	str	w9, [sp, #4]
	cbnz	w8, LBB2_3
	b	LBB2_2
LBB2_2:
	ldr	w8, [sp, #4]
	lsr	w0, w8, #0
	add	sp, sp, #16
	ret
LBB2_3:
	ldr	w9, [sp, #4]
	ldr	w10, [sp]
	subs	w8, w10, #1
	mul	w9, w9, w10
	str	w9, [sp, #8]
	str	w8, [sp, #12]
	b	LBB2_1
	.cfi_endproc

	.globl	_fib
	.p2align	2
_fib:
	.cfi_startproc
	sub	sp, sp, #48
	stp	x29, x30, [sp, #32]
	.cfi_def_cfa_offset 48
	.cfi_offset w30, -8
	.cfi_offset w29, -16
	mov	w8, wzr
	str	w8, [sp, #24]
	str	w0, [sp, #28]
	b	LBB3_1
LBB3_1:
	ldr	w9, [sp, #24]
	ldr	w8, [sp, #28]
	str	w8, [sp, #12]
	str	w9, [sp, #16]
	mov	w9, wzr
	str	w9, [sp, #20]
	cbz	w8, LBB3_3
	b	LBB3_2
LBB3_2:
	ldr	w8, [sp, #12]
	subs	w8, w8, #1
	b.eq	LBB3_4
	b	LBB3_5
LBB3_3:
	ldr	w8, [sp, #16]
	ldr	w9, [sp, #20]
	add	w0, w8, w9
	ldp	x29, x30, [sp, #32]
	add	sp, sp, #48
	ret
LBB3_4:
	mov	w8, #1
	str	w8, [sp, #20]
	b	LBB3_3
LBB3_5:
	ldr	w8, [sp, #12]
	subs	w0, w8, #1
	bl	_fib
	ldr	w8, [sp, #12]
	ldr	w9, [sp, #16]
	subs	w8, w8, #2
	add	w9, w9, w0
	str	w9, [sp, #24]
	str	w8, [sp, #28]
	b	LBB3_1
	.cfi_endproc

	.globl	_procBased
	.p2align	2
_procBased:
	.cfi_startproc
	mul	w8, w0, w0
	add	w8, w8, #1
	mul	w8, w8, w0
	add	w0, w8, #2
	ret
	.cfi_endproc

	.globl	_test
	.p2align	2
_test:
	.cfi_startproc
	sub	sp, sp, #16
	.cfi_def_cfa_offset 16
	mov	w8, wzr
	str	w8, [sp, #8]
	str	w0, [sp, #12]
	b	LBB5_1
LBB5_1:
	ldr	w9, [sp, #8]
	ldr	w8, [sp, #12]
	str	w8, [sp]
	str	w9, [sp, #4]
	cbnz	w8, LBB5_3
	b	LBB5_2
LBB5_2:
	ldr	w8, [sp, #4]
	add	w0, w8, #1
	add	sp, sp, #16
	ret
LBB5_3:
	ldr	w10, [sp, #4]
	ldr	w8, [sp]
	mul	w9, w8, w8
	add	w9, w9, w8
	subs	w8, w8, #1
	add	w9, w9, w10
	str	w9, [sp, #8]
	str	w8, [sp, #12]
	b	LBB5_1
	.cfi_endproc

	.globl	_lotsAndLots
	.p2align	2
_lotsAndLots:
	.cfi_startproc
	sub	sp, sp, #16
	.cfi_def_cfa_offset 16
	str	w0, [sp, #8]
	mov	w8, wzr
	str	w8, [sp, #12]
	cbz	w0, LBB6_6
	b	LBB6_1
LBB6_1:
	ldr	w8, [sp, #8]
	subs	w8, w8, #1
	b.eq	LBB6_7
	b	LBB6_2
LBB6_2:
	ldr	w8, [sp, #8]
	subs	w8, w8, #2
	b.eq	LBB6_8
	b	LBB6_3
LBB6_3:
	ldr	w8, [sp, #8]
	subs	w8, w8, #3
	b.eq	LBB6_9
	b	LBB6_4
LBB6_4:
	ldr	w8, [sp, #8]
	subs	w8, w8, #100
	b.eq	LBB6_10
	b	LBB6_5
LBB6_5:
	ldr	w8, [sp, #8]
	subs	w8, w8, #1000
	b.eq	LBB6_11
	b	LBB6_12
LBB6_6:
	ldr	w0, [sp, #12]
	add	sp, sp, #16
	ret
LBB6_7:
	mov	w8, #-9
	str	w8, [sp, #12]
	b	LBB6_6
LBB6_8:
	mov	w8, #4
	str	w8, [sp, #12]
	b	LBB6_6
LBB6_9:
	mov	w8, #27
	str	w8, [sp, #12]
	b	LBB6_6
LBB6_10:
	mov	w8, #110
	str	w8, [sp, #12]
	b	LBB6_6
LBB6_11:
	mov	w8, #1100
	str	w8, [sp, #12]
	b	LBB6_6
LBB6_12:
	ldr	w8, [sp, #8]
	tbz	w8, #31, LBB6_14
	b	LBB6_13
LBB6_13:
	ldr	w8, [sp, #8]
	add	w8, w8, #12
	str	w8, [sp, #12]
	b	LBB6_6
LBB6_14:
	ldr	w8, [sp, #8]
	add	w8, w8, #1
	str	w8, [sp, #12]
	b	LBB6_6
	.cfi_endproc

	.globl	_longFunc
	.p2align	2
_longFunc:
	.cfi_startproc
	mov	w8, #33920
	movk	w8, #30, lsl #16
	mul	w0, w0, w8
	ret
	.cfi_endproc

	.globl	_main
	.p2align	2
_main:
	.cfi_startproc
	sub	sp, sp, #48
	stp	x29, x30, [sp, #32]
	.cfi_def_cfa_offset 48
	.cfi_offset w30, -8
	.cfi_offset w29, -16
	adrp	x0, "l_num:"@PAGE
	add	x0, x0, "l_num:"@PAGEOFF
	bl	"_print:3str_t"
	mov	w0, #10
	str	w0, [sp, #28]
	bl	"_print:3i32_t"
	ldr	w0, [sp, #28]
	bl	_foo
	str	w0, [sp, #8]
	adrp	x0, "l_\nfoo"@PAGE
	add	x0, x0, "l_\nfoo"@PAGEOFF
	bl	"_print:3str_t"
	ldr	w0, [sp, #8]
	bl	"_print:3i32_t"
	ldr	w0, [sp, #8]
	bl	_bar
	str	w0, [sp, #12]
	adrp	x0, "l_\nbar"@PAGE
	add	x0, x0, "l_\nbar"@PAGEOFF
	bl	"_print:3str_t"
	ldr	w0, [sp, #12]
	bl	"_print:3i32_t"
	adrp	x0, "l_\n.1"@PAGE
	add	x0, x0, "l_\n.1"@PAGEOFF
	str	x0, [sp, #16]
	bl	"_print:3str_t"
	adrp	x0, "l_fac "@PAGE
	add	x0, x0, "l_fac "@PAGEOFF
	bl	"_print:3str_t"
	ldr	w0, [sp, #28]
	bl	_fac
	bl	"_print:3i32_t"
	adrp	x0, "l_\nfib"@PAGE
	add	x0, x0, "l_\nfib"@PAGEOFF
	bl	"_print:3str_t"
	ldr	w0, [sp, #28]
	bl	_fib
	bl	"_print:3i32_t"
	ldr	x0, [sp, #16]
	bl	"_print:3str_t"
	ldr	w0, [sp, #28]
	ldp	x29, x30, [sp, #32]
	add	sp, sp, #48
	b	_longFunc
	.cfi_endproc

	.section	__TEXT,__cstring,cstring_literals
"l_num:":
	.asciz	"num: "

"l_\nfoo":
	.asciz	"\nfoo num: "

"l_\nbar":
	.asciz	"\nbar foo num: "

"l_fac ":
	.asciz	"fac 10: "

"l_\nfib":
	.asciz	"\nfib 10: "

"l_\n.1":
	.asciz	"\n"

.subsections_via_symbols
