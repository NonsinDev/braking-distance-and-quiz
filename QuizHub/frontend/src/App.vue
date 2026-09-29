<script setup>
import { computed, ref } from 'vue'
import AdminView from './views/AdminView.vue'
import PlayerView from './views/PlayerView.vue'

const mode = ref(new URLSearchParams(window.location.search).get('mode') || '')
const isAdmin = computed(() => mode.value === 'admin')
</script>

<template>
  <main>
    <div v-if="!mode" class="mode-picker">
      <div class="brand-mark"><span>QH</span> QUIZHUB</div>
      <p class="eyebrow" style="margin-top: 24px;">LIVE MULTIPLAYER QUIZ</p>
      <h1>Wissen testen.<br /><em>Gemeinsam</em> spielen.</h1>
      <p class="intro">Das vielseitige Live-Quiz für Events, Partys, Unterricht und Wissensthemen aller Art — wie Kahoot, modular und direkt im Browser.</p>
      <div class="mode-actions">
        <button class="primary-action" @click="mode = 'admin'">Quiz Hosten <span>→</span></button>
        <button class="secondary-action" @click="mode = 'player'">Quiz Beitreten</button>
      </div>
    </div>
    <AdminView v-else-if="isAdmin" />
    <PlayerView v-else />
  </main>
</template>
