# Testes v0.2.4 (mundo descartavel)

1. Verificar log `v0.2.4` e inexistencia de `error CS`.
2. SCP F6, nenhum gatilho automatico em 2D (proximidade agora OFF por padrao).
3. Entrar no WorldFall com G; F10 com mira fora do SCP: AIM matchSCP=False.
4. F10 com mira no SCP de frente: AIM matchSCP=True esperado, FACE delta e limite visiveis.
5. Se delta for ~180 na frente, no F5 inverter offset para 180; repetir.
6. Manter a mira por 0,45s: Witness; a mesma sequencia nao deve duplicar.
7. Olhar nas costas: se estrito ligado, nao disparar.
8. Parede no meio: `matchSCP` deve ser falso; nunca marcar.
9. Se F10 `Sem heading do SCP nos caches`, logar; nao desligar modo estrito exceto teste isolado.
10. Se marcado, confirmar perseguicao e morte dentro de 1,7 tiles como v0.1.6.
11. Testar F7 e F11 para regressao; verificar fila FIFO.
12. Confirmar WorldFall desligado: mod continua spawn/perseguicao manual.
